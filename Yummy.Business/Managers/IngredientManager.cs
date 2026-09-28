using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Yummy.Core.DTOs.CommonDTOs;
using Yummy.Core.DTOs.IngredientDTOs;
using Yummy.Core.Exceptions;
using Yummy.Core.IRepositories;
using Yummy.Core.IUnitOfWork;
using Yummy.Core.Services;
using Yummy.Entity;
using Yummy.Entity.Enums;

namespace Yummy.Business.Managers
{
    // stok kartları ve stok hareketleri.
    // eşzamanlılık kuralı: stok kartını veya stoğu değiştiren her işlem aynı "stock" kilidini alır ve kaydı kilidin İÇİNDE yeniden okur.
    // böylece iki işlem aynı stoğu eski bir kopya üzerinden değiştiremez (örn. iki fire kaydı stoğu eksiye düşüremez).
    // stoğu değiştiren diğer işlemler (IngredientRequestManager: talep tedariki; ilerideki adımda günün spesyali) de aynı kilidi kullanır.
    public class IngredientManager : IIngredientService
    {
        private readonly IGenericRepository<Ingredient> _ingredientRepository;
        private readonly IGenericRepository<StockMovement> _stockMovementRepository;
        private readonly IGenericRepository<IngredientRequestItem> _requestItemRepository;
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        internal const string StockLockKey = "stock";

        // malzeme adları büyük/küçük harf duyarsız ve Türkçe kurallarına göre karşılaştırılır ("incir" ile "İNCİR" aynı malzemedir).
        private static readonly StringComparer NameComparer = StringComparer.Create(CultureInfo.GetCultureInfo("tr-TR"), ignoreCase: true);

        public IngredientManager(IGenericRepository<Ingredient> ingredientRepository, IGenericRepository<StockMovement> stockMovementRepository, IGenericRepository<IngredientRequestItem> requestItemRepository, IUnitOfWork uow, IMapper mapper)
        {
            _ingredientRepository = ingredientRepository;
            _stockMovementRepository = stockMovementRepository;
            _requestItemRepository = requestItemRepository;
            _uow = uow;
            _mapper = mapper;
        }

        public async Task<IEnumerable<IngredientListDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var ingredients = await _ingredientRepository.GetAllAsync(cancellationToken);
            return _mapper.Map<IEnumerable<IngredientListDto>>(ingredients.OrderBy(i => i.Name, NameComparer));
        }

        public async Task<IngredientListDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var ingredient = await _ingredientRepository.GetByIdAsync(id, cancellationToken)
                ?? throw new LogicException("NotFound", "Malzeme bulunamadı.");

            return _mapper.Map<IngredientListDto>(ingredient);
        }

        public async Task AddAsync(IngredientCreateDto dto, CancellationToken cancellationToken = default)
        {
            var name = dto.Name.Trim();

            await _uow.ExecuteInLockedTransactionAsync(StockLockKey, async () =>
            {
                await EnsureNameIsUniqueAsync(name, null, cancellationToken);

                await _ingredientRepository.AddAsync(new Ingredient
                {
                    IngredientId = Guid.NewGuid(),
                    Name = name,
                    Unit = dto.Unit,
                    StockQuantity = 0
                }, cancellationToken);
                await _uow.SaveAsync(cancellationToken);
            }, cancellationToken);
        }

        // sadece ad ve birim güncellenir. kayıt kilit içinde okunduğu için Update, aynı anda yapılan bir stok hareketinin miktarını eski değerle ezmez.
        public async Task UpdateAsync(IngredientUpdateDto dto, CancellationToken cancellationToken = default)
        {
            var name = dto.Name.Trim();

            await _uow.ExecuteInLockedTransactionAsync(StockLockKey, async () =>
            {
                var ingredient = await _ingredientRepository.GetByIdAsync(dto.IngredientId, cancellationToken)
                    ?? throw new LogicException("NotFound", "Güncellenecek malzeme bulunamadı.");

                await EnsureNameIsUniqueAsync(name, ingredient.IngredientId, cancellationToken);

                // hareket geçmişindeki ve taleplerdeki miktarlar eski birime göredir; birim değişirse "5 kg" kaydı "5 g" olarak okunur.
                if (ingredient.Unit != dto.Unit &&
                    (await _stockMovementRepository.AnyAsync(m => m.IngredientId == ingredient.IngredientId, cancellationToken) ||
                     await _requestItemRepository.AnyAsync(i => i.IngredientId == ingredient.IngredientId, cancellationToken)))
                    throw new LogicException("UnitLocked", "Stok hareketi veya talebi bulunan bir malzemenin birimi değiştirilemez. Farklı birimle yeni bir malzeme kartı oluşturunuz.");

                ingredient.Name = name;
                ingredient.Unit = dto.Unit;

                _ingredientRepository.Update(ingredient);
                await _uow.SaveAsync(cancellationToken);
            }, cancellationToken);
        }

        // malzeme soft delete ile silinir; hareket geçmişi korunur. stokta miktar varken silinemez (önce fire veya sayım ile sıfırlanmalıdır).
        // bekleyen bir talepte yer alan malzeme de silinemez; aksi halde çalışan talebi, artık olmayan bir kartın stoğuna tedarik etmeye çalışır.
        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            await _uow.ExecuteInLockedTransactionAsync(StockLockKey, async () =>
            {
                var ingredient = await _ingredientRepository.GetByIdAsync(id, cancellationToken)
                    ?? throw new LogicException("NotFound", "Silinecek malzeme bulunamadı.");

                if (ingredient.StockQuantity != 0)
                    throw new LogicException("StockNotEmpty", "Stokta miktar bulunan bir malzeme silinemez. Önce stoğu fire veya sayım düzeltmesi ile sıfırlayınız.");

                if (await _requestItemRepository.AnyAsync(i => i.IngredientId == id && i.IngredientRequest.Status == IngredientRequestStatus.Pending, cancellationToken))
                    throw new LogicException("IngredientInPendingRequest", "Bu malzeme bekleyen bir malzeme talebinde yer aldığı için silinemez. Önce talebin sonuçlandırılması gerekir.");

                _ingredientRepository.Remove(ingredient);
                await _uow.SaveAsync(cancellationToken);
            }, cancellationToken);
        }

        // çalışanın elle yaptığı stok hareketi (giriş, fire, sayım). stok bunun dışında sadece talep tedariki (ve ilerideki adımda spesyal) ile değişir;
        // her değişiklik bir hareket kaydı bırakır.
        public async Task AdjustStockAsync(string userId, Guid ingredientId, StockAdjustmentDto dto, CancellationToken cancellationToken = default)
        {
            if (!Guid.TryParse(userId, out Guid parsedUserId))
                throw new LogicException("InvalidUserId", "Kullanıcı kimliği geçersiz.");

            await _uow.ExecuteInLockedTransactionAsync(StockLockKey, async () =>
            {
                var ingredient = await _ingredientRepository.GetByIdAsync(ingredientId, cancellationToken)
                    ?? throw new LogicException("NotFound", "Malzeme bulunamadı.");

                var (movementType, change) = dto.Type switch
                {
                    StockAdjustmentType.StockIn => (StockMovementType.StockIn, dto.Quantity),
                    StockAdjustmentType.Waste => (StockMovementType.Waste, -dto.Quantity),
                    StockAdjustmentType.CountCorrection => (StockMovementType.CountCorrection, dto.Quantity - ingredient.StockQuantity),
                    _ => throw new LogicException("InvalidType", "Geçersiz stok hareketi türü.")
                };

                if (ingredient.StockQuantity + change < 0)
                    throw new LogicException("InsufficientStock", $"Stokta yeterli miktar yok. Mevcut stok: {ingredient.StockQuantity:0.###}.");

                if (change == 0)
                    throw new LogicException("NoChanges", "Sayılan miktar mevcut stok ile aynı; değişiklik yapılmadı.");

                ingredient.StockQuantity += change;
                _ingredientRepository.Update(ingredient);

                await _stockMovementRepository.AddAsync(new StockMovement
                {
                    StockMovementId = Guid.NewGuid(),
                    IngredientId = ingredient.IngredientId,
                    Type = movementType,
                    QuantityChange = change,
                    QuantityAfter = ingredient.StockQuantity,
                    Note = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim(),
                    PerformedByUserId = parsedUserId
                }, cancellationToken);

                await _uow.SaveAsync(cancellationToken);
            }, cancellationToken);
        }

        // malzemenin hareket geçmişi, en yeni hareket önce. sayfalı döner.
        public async Task<PagedResultDto<StockMovementListDto>> GetStockMovementsAsync(Guid ingredientId, PaginationQueryDto query, CancellationToken cancellationToken = default)
        {
            if (!await _ingredientRepository.AnyAsync(i => i.IngredientId == ingredientId, cancellationToken))
                throw new LogicException("NotFound", "Malzeme bulunamadı.");

            var (movements, totalCount) = await _stockMovementRepository.GetPagedAsync(
                m => m.IngredientId == ingredientId,
                q => q.OrderByDescending(m => m.CreatedDate).ThenBy(m => m.StockMovementId),
                query.Page,
                query.PageSize,
                cancellationToken,
                m => m.PerformedByUser!);

            return new PagedResultDto<StockMovementListDto>
            {
                Items = _mapper.Map<List<StockMovementListDto>>(movements),
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = totalCount
            };
        }

        // silinmemiş malzemeler arasında ad benzersizdir. karşılaştırma Türkçe kurallarına göre yapıldığı için bellekte yapılır;
        // veritabanının harf karşılaştırma kuralı (collation) provider'a göre değiştiği için sorguya bırakılmaz. stok kartı sayısı küçüktür.
        private async Task EnsureNameIsUniqueAsync(string name, Guid? excludeIngredientId, CancellationToken cancellationToken)
        {
            var ingredients = await _ingredientRepository.GetAllAsync(cancellationToken);
            if (ingredients.Any(i => i.IngredientId != excludeIngredientId && NameComparer.Equals(i.Name, name)))
                throw new LogicException("Name", $"'{name}' adında bir malzeme zaten mevcut.");
        }
    }
}
