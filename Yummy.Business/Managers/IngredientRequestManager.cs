using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Yummy.Core.DTOs.CommonDTOs;
using Yummy.Core.DTOs.IngredientRequestDTOs;
using Yummy.Core.Exceptions;
using Yummy.Core.IRepositories;
using Yummy.Core.IUnitOfWork;
using Yummy.Core.Services;
using Yummy.Entity;
using Yummy.Entity.Enums;

namespace Yummy.Business.Managers
{
    // şefin malzeme talepleri: şef talep oluşturur, çalışan (Employee) tedarik eder veya reddeder, şef e-posta ile bilgilendirilir.
    // eşzamanlılık kuralı: talebin durumunu veya stoğu değiştiren her işlem IngredientManager ile aynı "stock" kilidini alır ve talebi kilidin İÇİNDE
    // yeniden okur. böylece aynı talep iki kez tedarik edilemez, şefin iptali ile çalışanın tedariki çakışamaz ve stok hesabı tutarlı kalır.
    public class IngredientRequestManager : IIngredientRequestService
    {
        private readonly IGenericRepository<IngredientRequest> _requestRepository;
        private readonly IGenericRepository<Ingredient> _ingredientRepository;
        private readonly IGenericRepository<StockMovement> _stockMovementRepository;
        private readonly IGenericRepository<Chef> _chefRepository;
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailService;
        private readonly ILogger<IngredientRequestManager> _logger;
        private readonly TimeProvider _timeProvider;

        private static readonly CultureInfo TurkishCulture = CultureInfo.GetCultureInfo("tr-TR");

        public IngredientRequestManager(IGenericRepository<IngredientRequest> requestRepository, IGenericRepository<Ingredient> ingredientRepository,
            IGenericRepository<StockMovement> stockMovementRepository, IGenericRepository<Chef> chefRepository, IUnitOfWork uow, IMapper mapper,
            IEmailService emailService, ILogger<IngredientRequestManager> logger, TimeProvider timeProvider)
        {
            _requestRepository = requestRepository;
            _ingredientRepository = ingredientRepository;
            _stockMovementRepository = stockMovementRepository;
            _chefRepository = chefRepository;
            _uow = uow;
            _mapper = mapper;
            _emailService = emailService;
            _logger = logger;
            _timeProvider = timeProvider;
        }

        #region Şef

        // talep, stok kilidi altında oluşturulur: aynı anda silinen bir malzeme veya şef profili bekleyen bir talepte kalamaz
        // (malzeme ve şef silme işlemleri de bu kilidi alır ve bekleyen talepleri kontrol eder).
        public async Task<Guid> CreateAsync(string userId, IngredientRequestCreateDto dto, CancellationToken cancellationToken = default)
        {
            var parsedUserId = ParseUserId(userId);
            var requestId = Guid.NewGuid();

            await _uow.ExecuteInLockedTransactionAsync(IngredientManager.StockLockKey, async () =>
            {
                var chef = await GetLinkedChefAsync(parsedUserId, cancellationToken);

                var ingredientIds = dto.Items.Select(i => i.IngredientId).ToList();
                var ingredients = (await _ingredientRepository.GetWhereAsync(i => ingredientIds.Contains(i.IngredientId), cancellationToken))
                    .ToDictionary(i => i.IngredientId);

                if (ingredients.Count != ingredientIds.Distinct().Count())
                    throw new LogicException("IngredientNotFound", "Talep edilen malzemelerden bazıları bulunamadı veya silinmiş. Lütfen malzeme listesini yenileyiniz.");

                var request = new IngredientRequest
                {
                    IngredientRequestId = requestId,
                    ChefId = chef.ChefId,
                    ChefName = $"{chef.Name} {chef.Surname}",
                    ChefNote = NormalizeNote(dto.Note),
                    Status = IngredientRequestStatus.Pending,
                    Items = dto.Items.Select(item => new IngredientRequestItem
                    {
                        IngredientRequestItemId = Guid.NewGuid(),
                        IngredientId = item.IngredientId,
                        IngredientName = ingredients[item.IngredientId].Name,
                        Unit = ingredients[item.IngredientId].Unit,
                        RequestedQuantity = item.Quantity
                    }).ToList()
                };

                await _requestRepository.AddAsync(request, cancellationToken);
                await _uow.SaveAsync(cancellationToken);
            }, cancellationToken);

            return requestId;
        }

        public async Task<PagedResultDto<IngredientRequestListDto>> GetMyRequestsAsync(string userId, IngredientRequestQueryDto query, CancellationToken cancellationToken = default)
        {
            var chef = await GetLinkedChefAsync(ParseUserId(userId), cancellationToken);
            var status = query.Status;

            return await GetPagedAsync(r => r.ChefId == chef.ChefId && (status == null || r.Status == status), query, cancellationToken);
        }

        // şef sadece kendi talebini görebilir; başka bir şefin talebi "bulunamadı" olarak döner (varlığı da açığa çıkmaz).
        public async Task<IngredientRequestListDto> GetMyRequestByIdAsync(string userId, Guid requestId, CancellationToken cancellationToken = default)
        {
            var chef = await GetLinkedChefAsync(ParseUserId(userId), cancellationToken);

            var request = await _requestRepository.GetSingleAsync(r => r.IngredientRequestId == requestId && r.ChefId == chef.ChefId, cancellationToken, r => r.Items, r => r.HandledByUser!)
                ?? throw new LogicException("NotFound", "Malzeme talebi bulunamadı.");

            return _mapper.Map<IngredientRequestListDto>(request);
        }

        // şef sadece bekleyen (henüz sonuçlanmamış) talebini iptal edebilir.
        public async Task CancelAsync(string userId, Guid requestId, CancellationToken cancellationToken = default)
        {
            var parsedUserId = ParseUserId(userId);

            await _uow.ExecuteInLockedTransactionAsync(IngredientManager.StockLockKey, async () =>
            {
                var chef = await GetLinkedChefAsync(parsedUserId, cancellationToken);

                var request = await _requestRepository.GetSingleAsync(r => r.IngredientRequestId == requestId && r.ChefId == chef.ChefId, cancellationToken)
                    ?? throw new LogicException("NotFound", "Malzeme talebi bulunamadı.");

                EnsurePending(request);

                request.Status = IngredientRequestStatus.Cancelled;
                _requestRepository.Update(request);
                await _uow.SaveAsync(cancellationToken);
            }, cancellationToken);
        }

        #endregion

        #region Çalışan

        // bekleyen talepleri görmek için ?status=Pending kullanılır. en yeni talep önce gelir.
        public async Task<PagedResultDto<IngredientRequestListDto>> GetAllAsync(IngredientRequestQueryDto query, CancellationToken cancellationToken = default)
        {
            var status = query.Status;
            return await GetPagedAsync(r => status == null || r.Status == status, query, cancellationToken);
        }

        public async Task<IngredientRequestListDto> GetByIdAsync(Guid requestId, CancellationToken cancellationToken = default)
        {
            var request = await _requestRepository.GetSingleAsync(r => r.IngredientRequestId == requestId, cancellationToken, r => r.Items, r => r.HandledByUser!)
                ?? throw new LogicException("NotFound", "Malzeme talebi bulunamadı.");

            return _mapper.Map<IngredientRequestListDto>(request);
        }

        // çalışan her satır için gerçekte gelen miktarı girer. gelen miktar stoğa eklenir ve her malzeme için "talep tedariki" hareketi kaydedilir.
        // talep tek seferde sonuçlanır (kısmi tedarik ayrı bir durum değildir; şef satır bazında neyin ne kadar geldiğini görür).
        public async Task SupplyAsync(string userId, Guid requestId, IngredientRequestSupplyDto dto, CancellationToken cancellationToken = default)
        {
            var parsedUserId = ParseUserId(userId);
            IngredientRequest request = null!;

            await _uow.ExecuteInLockedTransactionAsync(IngredientManager.StockLockKey, async () =>
            {
                request = await GetRequestForUpdateAsync(requestId, cancellationToken);
                EnsurePending(request);

                // gönderilen satırlar talebin satırlarıyla birebir aynı olmalıdır; eksik satır "unutuldu" mu "bulunamadı" mı belli olmaz.
                var supplied = dto.Items.ToDictionary(i => i.IngredientRequestItemId, i => i.SuppliedQuantity);
                if (supplied.Count != request.Items.Count || request.Items.Any(item => !supplied.ContainsKey(item.IngredientRequestItemId)))
                    throw new LogicException("ItemsMismatch", "Tedarik bilgisi talepteki her malzeme için bir kez girilmelidir (bulunamayan malzeme için 0 giriniz).");

                foreach (var item in request.Items)
                {
                    var quantity = supplied[item.IngredientRequestItemId];
                    item.SuppliedQuantity = quantity;

                    if (quantity == 0)
                        continue;

                    // bekleyen talepteki malzeme silinemez (IngredientManager.DeleteAsync); yine de kart bulunamazsa stok sessizce kaybolmasın diye işlem durdurulur.
                    var ingredient = await _ingredientRepository.GetByIdAsync(item.IngredientId, cancellationToken)
                        ?? throw new LogicException("IngredientNotFound", $"'{item.IngredientName}' malzemesinin stok kartı bulunamadı.");

                    ingredient.StockQuantity += quantity;
                    _ingredientRepository.Update(ingredient);

                    await _stockMovementRepository.AddAsync(new StockMovement
                    {
                        StockMovementId = Guid.NewGuid(),
                        IngredientId = ingredient.IngredientId,
                        Type = StockMovementType.RequestSupply,
                        QuantityChange = quantity,
                        QuantityAfter = ingredient.StockQuantity,
                        Note = $"Malzeme talebi: {request.ChefName}",
                        PerformedByUserId = parsedUserId,
                        IngredientRequestId = request.IngredientRequestId
                    }, cancellationToken);
                }

                Complete(request, IngredientRequestStatus.Supplied, parsedUserId, dto.Note);
                _requestRepository.Update(request);
                await _uow.SaveAsync(cancellationToken);
            }, cancellationToken);

            await NotifyChefAsync(request, "Malzemeleriniz Hazır", "tedarik edilmiştir. Malzemeler stoğa eklendi", "#28a745", cancellationToken);
        }

        public async Task RejectAsync(string userId, Guid requestId, IngredientRequestRejectDto dto, CancellationToken cancellationToken = default)
        {
            var parsedUserId = ParseUserId(userId);
            IngredientRequest request = null!;

            await _uow.ExecuteInLockedTransactionAsync(IngredientManager.StockLockKey, async () =>
            {
                request = await GetRequestForUpdateAsync(requestId, cancellationToken);
                EnsurePending(request);

                Complete(request, IngredientRequestStatus.Rejected, parsedUserId, dto.Note);
                _requestRepository.Update(request);
                await _uow.SaveAsync(cancellationToken);
            }, cancellationToken);

            await NotifyChefAsync(request, "Talebiniz Reddedildi", "reddedilmiştir", "#dc3545", cancellationToken);
        }

        #endregion

        #region Yardımcılar

        private static Guid ParseUserId(string userId) =>
            Guid.TryParse(userId, out Guid parsedUserId) ? parsedUserId : throw new LogicException("InvalidUserId", "Kullanıcı kimliği geçersiz.");

        // token'daki kullanıcıya bağlı şef profili. Chef rolü olsa bile bağlı bir profili olmayan kullanıcı talep işlemi yapamaz.
        private async Task<Chef> GetLinkedChefAsync(Guid userId, CancellationToken cancellationToken) =>
            await _chefRepository.GetSingleAsync(c => c.AppUserId == userId, cancellationToken)
                ?? throw new LogicException("ChefProfileNotFound", "Hesabınız herhangi bir şef profiline bağlı değil. Lütfen yönetici ile iletişime geçiniz.");

        private async Task<IngredientRequest> GetRequestForUpdateAsync(Guid requestId, CancellationToken cancellationToken) =>
            await _requestRepository.GetSingleAsync(r => r.IngredientRequestId == requestId, cancellationToken, r => r.Items)
                ?? throw new LogicException("NotFound", "Malzeme talebi bulunamadı.");

        // sadece bekleyen talep sonuçlandırılabilir veya iptal edilebilir; durum kilit içinde güncel kayıt üzerinden kontrol edilir.
        private static void EnsurePending(IngredientRequest request)
        {
            if (request.Status == IngredientRequestStatus.Pending)
                return;

            var current = request.Status switch
            {
                IngredientRequestStatus.Supplied => "tedarik edilmiş",
                IngredientRequestStatus.Rejected => "reddedilmiş",
                IngredientRequestStatus.Cancelled => "iptal edilmiş",
                _ => "sonuçlanmış"
            };
            throw new LogicException("NotPending", $"Bu talep zaten {current}. Sadece bekleyen talepler üzerinde işlem yapılabilir.");
        }

        private void Complete(IngredientRequest request, IngredientRequestStatus status, Guid handledByUserId, string? note)
        {
            request.Status = status;
            request.ResponseNote = NormalizeNote(note);
            request.HandledByUserId = handledByUserId;
            request.HandledDate = _timeProvider.GetUtcNow().UtcDateTime;
        }

        private static string? NormalizeNote(string? note) => string.IsNullOrWhiteSpace(note) ? null : note.Trim();

        private async Task<PagedResultDto<IngredientRequestListDto>> GetPagedAsync(Expression<Func<IngredientRequest, bool>> predicate, IngredientRequestQueryDto query, CancellationToken cancellationToken)
        {
            var (requests, totalCount) = await _requestRepository.GetPagedAsync(
                predicate,
                q => q.OrderByDescending(r => r.CreatedDate).ThenBy(r => r.IngredientRequestId),
                query.Page,
                query.PageSize,
                cancellationToken,
                r => r.Items,
                r => r.HandledByUser!);

            return new PagedResultDto<IngredientRequestListDto>
            {
                Items = _mapper.Map<List<IngredientRequestListDto>>(requests),
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = totalCount
            };
        }

        // şef, talebinin sonucunu hesabına bağlı e-posta adresinden öğrenir. e-posta, talep veritabanına kaydedildikten sonra gönderilir;
        // şablon bulunamaz, SMTP hata verir veya şefin bağlı hesabı yoksa işlem geri alınmaz, durum loglanır (şef sonucu panelinden de görür).
        private async Task NotifyChefAsync(IngredientRequest request, string statusTitle, string statusMessage, string statusColor, CancellationToken cancellationToken)
        {
            try
            {
                var chef = await _chefRepository.GetSingleAsync(c => c.ChefId == request.ChefId, cancellationToken, c => c.AppUser!);
                var email = chef?.AppUser?.Email;
                if (string.IsNullOrEmpty(email))
                {
                    _logger.LogInformation("Malzeme talebi sonucu e-postası gönderilmedi; şefin bağlı bir hesabı yok. Talep: {RequestId}", request.IngredientRequestId);
                    return;
                }

                var templatePath = Path.Combine(AppContext.BaseDirectory, "Templates", "IngredientRequestResultTemplate.html");
                var mailBody = await File.ReadAllTextAsync(templatePath, cancellationToken);

                var createdLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(request.CreatedDate, DateTimeKind.Utc), _timeProvider.LocalTimeZone);

                // kullanıcıdan gelen tüm değerler (ad, not, malzeme adı) HTML olarak encode edilir; satır tablosu da encode edilmiş değerlerle oluşturulur.
                var placeholders = new Dictionary<string, string>
                {
                    ["{{ChefName}}"] = Encode(request.ChefName),
                    ["{{StatusTitle}}"] = Encode(statusTitle),
                    ["{{StatusMessage}}"] = Encode(statusMessage),
                    ["#112233"] = statusColor,
                    ["{{RequestDate}}"] = Encode(createdLocal.ToString("dd.MM.yyyy HH:mm", TurkishCulture)),
                    ["{{ResponseNote}}"] = Encode(request.ResponseNote ?? "-"),
                    ["{{ItemRows}}"] = BuildItemRows(request)
                };

                foreach (var placeholder in placeholders)
                    mailBody = mailBody.Replace(placeholder.Key, placeholder.Value);

                await _emailService.SendEmailAsync(email, $"Yummy Restoran - Malzeme Talebi ({statusTitle})", mailBody, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Malzeme talebi sonucu e-postası gönderilemedi. Talep: {RequestId}", request.IngredientRequestId);
            }
        }

        private static string BuildItemRows(IngredientRequest request)
        {
            var rows = new StringBuilder();
            foreach (var item in request.Items.OrderBy(i => i.IngredientName, StringComparer.Create(TurkishCulture, ignoreCase: true)))
            {
                var unit = item.Unit switch
                {
                    IngredientUnit.Gram => "g",
                    IngredientUnit.Kilogram => "kg",
                    IngredientUnit.Milliliter => "ml",
                    IngredientUnit.Liter => "L",
                    _ => "adet"
                };
                var supplied = item.SuppliedQuantity.HasValue ? $"{item.SuppliedQuantity.Value.ToString("0.###", TurkishCulture)} {unit}" : "-";

                rows.Append("<tr>")
                    .Append($"<td style=\"padding: 8px; border-bottom: 1px solid #e2e8f0;\">{Encode(item.IngredientName)}</td>")
                    .Append($"<td style=\"padding: 8px; border-bottom: 1px solid #e2e8f0; text-align: right;\">{Encode($"{item.RequestedQuantity.ToString("0.###", TurkishCulture)} {unit}")}</td>")
                    .Append($"<td style=\"padding: 8px; border-bottom: 1px solid #e2e8f0; text-align: right;\">{Encode(supplied)}</td>")
                    .Append("</tr>");
            }
            return rows.ToString();
        }

        private static string Encode(string value) => WebUtility.HtmlEncode(value);

        #endregion
    }
}
