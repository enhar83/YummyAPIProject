using System.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Yummy.Core.Constants;
using Yummy.Core.DTOs.ChefDTOs;
using Yummy.Core.Exceptions;
using Yummy.Core.IRepositories;
using Yummy.Core.IUnitOfWork;
using Yummy.Core.Services;
using Yummy.Entity;
using Yummy.Entity.Enums;

namespace Yummy.Business.Managers
{
    // şef profilleri (vitrin) ve şef profili ↔ kullanıcı hesabı bağlantısı.
    // bağlantı kurulan kullanıcıya Chef rolü verilir; bağlantı kaldırılınca veya şef silinince rol geri alınır. böylece rol ve bağlantı her zaman birlikte değişir.
    public class ChefManager : IChefService
    {
        private readonly IGenericRepository<Chef> _chefRepository;
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private IWebHostEnvironment _environment;
        private readonly UserManager<AppUser> _userManager;
        private readonly IGenericRepository<IngredientRequest> _requestRepository;

        // bağlantı işlemleri tek bir kilit altında sıraya girer (düşük trafikli admin işlemi). aynı kullanıcının aynı anda iki profile bağlanması
        // unique index ile de engellenir; kilit bu durumda 500 yerine anlaşılır bir hata mesajı dönülmesini sağlar.
        private const string ChefUserLinkLockKey = "chef-user-link";

        public ChefManager(IGenericRepository<Chef> chefRepository, IUnitOfWork uow, IMapper mapper, IWebHostEnvironment environment, UserManager<AppUser> userManager, IGenericRepository<IngredientRequest> requestRepository)
        {
            _chefRepository = chefRepository;
            _uow = uow;
            _mapper = mapper;
            _environment = environment;
            _userManager = userManager;
            _requestRepository = requestRepository;
        }

        public async Task AddAsync(ChefCreateDto dto, CancellationToken cancellationToken = default)
        {
            var chef = _mapper.Map<Chef>(dto);
            chef.ChefId = Guid.NewGuid();

            if (dto.Image != null)
                chef.ImageUrl = await SaveFileAsync(dto.Image);

            await _chefRepository.AddAsync(chef, cancellationToken);
            await _uow.SaveAsync(cancellationToken);
        }

        // şef soft delete ile silinir. hesabı bağlıysa önce bağlantı kaldırılır ve Chef rolü geri alınır;
        // aksi halde silinmiş şefin kullanıcısı şef paneline erişmeye devam eder ve unique index nedeniyle başka bir profile bağlanamaz.
        // şefin bekleyen malzeme talepleri iptal edilir; aksi halde çalışanlar artık olmayan bir şef için tedarik yapar.
        // talepler stok kilidi altında değiştiği için o kilit de alınır (sıra: önce bağlantı, sonra stok kilidi; başka hiçbir işlem ters sırayla almaz).
        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            Chef chef = null!;

            await _uow.ExecuteInLockedTransactionAsync(new[] { ChefUserLinkLockKey, IngredientManager.StockLockKey }, async () =>
            {
                chef = await _chefRepository.GetByIdAsync(id, cancellationToken)
                    ?? throw new LogicException("ChefId", "Silinmek istenen şef bulunamadı.");

                if (chef.AppUserId.HasValue)
                    await RemoveUserLinkAsync(chef);

                var pendingRequests = await _requestRepository.GetWhereAsync(r => r.ChefId == chef.ChefId && r.Status == IngredientRequestStatus.Pending, cancellationToken);
                foreach (var request in pendingRequests)
                {
                    request.Status = IngredientRequestStatus.Cancelled;
                    request.ResponseNote = "Şef profili silindiği için talep iptal edildi.";
                    _requestRepository.Update(request);
                }

                _chefRepository.Remove(chef);
                await _uow.SaveAsync(cancellationToken);
            }, cancellationToken);

            DeleteFile(chef.ImageUrl); // dosya, kayıt işlemi başarıyla tamamlandıktan sonra silinir.
        }

        public async Task<IEnumerable<ChefResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var entities = await _chefRepository.GetAllAsync(cancellationToken, c => c.AppUser!);
            return _mapper.Map<IEnumerable<ChefResponseDto>>(entities);
        }

        public async Task<ChefResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var chef = await _chefRepository.GetSingleAsync(c => c.ChefId == id, cancellationToken, c => c.AppUser!);
            if (chef == null)
                throw new LogicException("ChefId", "Aradığınız şef bulunamadı.");

            return _mapper.Map<ChefResponseDto>(chef);
        }

        // Update tüm kolonları yazdığı için bağlantı işlemleriyle aynı kilit altında ve güncel kayıt üzerinde yapılır;
        // aksi halde aynı anda kurulan bir bağlantının AppUserId değeri eski kopya ile ezilebilir.
        public async Task UpdateAsync(ChefUpdateDto dto, CancellationToken cancellationToken = default)
        {
            await _uow.ExecuteInLockedTransactionAsync(ChefUserLinkLockKey, async () =>
            {
                var chef = await _chefRepository.GetByIdAsync(dto.ChefId, cancellationToken);
                if (chef == null)
                    throw new LogicException("ChefId", "Güncellenmek istenen şef bulunamadı.");

                _mapper.Map(dto, chef);

                if (dto.Image != null)
                {
                    DeleteFile(chef.ImageUrl);
                    chef.ImageUrl = await SaveFileAsync(dto.Image);
                }

                _chefRepository.Update(chef);
                await _uow.SaveAsync(cancellationToken);
            }, cancellationToken);
        }

        // şef profilini bir kullanıcı hesabına bağlar ve kullanıcıya Chef rolü verir.
        // şef profili zaten başka bir hesaba bağlıysa veya kullanıcı başka bir şef profiline bağlıysa reddedilir (önce mevcut bağlantı kaldırılmalıdır).
        public async Task LinkUserAsync(Guid chefId, ChefLinkUserDto dto, CancellationToken cancellationToken = default)
        {
            await _uow.ExecuteInLockedTransactionAsync(ChefUserLinkLockKey, async () =>
            {
                var chef = await _chefRepository.GetByIdAsync(chefId, cancellationToken)
                    ?? throw new LogicException("ChefId", "Şef bulunamadı.");

                if (chef.AppUserId == dto.UserId)
                    throw new LogicException("AlreadyLinked", "Bu şef profili zaten bu kullanıcıya bağlı.");

                if (chef.AppUserId.HasValue)
                    throw new LogicException("ChefAlreadyLinked", "Bu şef profili başka bir kullanıcıya bağlı. Önce mevcut bağlantıyı kaldırınız.");

                var user = await _userManager.FindByIdAsync(dto.UserId.ToString())
                    ?? throw new LogicException("UserNotFound", "Kullanıcı sistemde bulunamadı.");

                if (await _chefRepository.AnyAsync(c => c.AppUserId == dto.UserId, cancellationToken))
                    throw new LogicException("UserAlreadyLinked", "Bu kullanıcı başka bir şef profiline bağlı. Önce mevcut bağlantıyı kaldırınız.");

                chef.AppUserId = user.Id;
                _chefRepository.Update(chef);
                await _uow.SaveAsync(cancellationToken);

                if (!await _userManager.IsInRoleAsync(user, RoleNames.Chef))
                {
                    // roller token içerisine gömüldüğü için kullanıcının oturumu sonlandırılır; tekrar giriş yaptığında token'ında Chef rolü bulunur.
                    RevokeSessions(user);
                    EnsureSucceeded(await _userManager.AddToRoleAsync(user, RoleNames.Chef), "LinkUserFailed");
                }
            }, cancellationToken);
        }

        public async Task UnlinkUserAsync(Guid chefId, CancellationToken cancellationToken = default)
        {
            await _uow.ExecuteInLockedTransactionAsync(ChefUserLinkLockKey, async () =>
            {
                var chef = await _chefRepository.GetByIdAsync(chefId, cancellationToken)
                    ?? throw new LogicException("ChefId", "Şef bulunamadı.");

                if (!chef.AppUserId.HasValue)
                    throw new LogicException("NotLinked", "Bu şef profili herhangi bir kullanıcıya bağlı değil.");

                await RemoveUserLinkAsync(chef);
                _chefRepository.Update(chef);
                await _uow.SaveAsync(cancellationToken);
            }, cancellationToken);
        }

        // şef paneli: token'daki kullanıcıya bağlı şef profili döner. kullanıcının Chef rolü olsa bile bağlı bir profili yoksa panel kullanılamaz.
        public async Task<ChefResponseDto> GetMyProfileAsync(string userId, CancellationToken cancellationToken = default)
        {
            if (!Guid.TryParse(userId, out Guid parsedUserId))
                throw new LogicException("InvalidUserId", "Kullanıcı kimliği geçersiz.");

            var chef = await _chefRepository.GetSingleAsync(c => c.AppUserId == parsedUserId, cancellationToken, c => c.AppUser!)
                ?? throw new LogicException("ChefProfileNotFound", "Hesabınız herhangi bir şef profiline bağlı değil. Lütfen yönetici ile iletişime geçiniz.");

            return _mapper.Map<ChefResponseDto>(chef);
        }

        // bağlantıyı kaldırır ve kullanıcının Chef rolünü geri alır (kullanıcı silinmişse sadece bağlantı kaldırılır). kaydetme işlemi çağıran metottadır.
        private async Task RemoveUserLinkAsync(Chef chef)
        {
            var user = await _userManager.FindByIdAsync(chef.AppUserId!.Value.ToString());
            chef.AppUserId = null;

            if (user != null && await _userManager.IsInRoleAsync(user, RoleNames.Chef))
            {
                RevokeSessions(user); // kaldırılan rol, eski token'larda veya refresh ile yeniden üretilen token'larda kalmasın diye.
                EnsureSucceeded(await _userManager.RemoveFromRoleAsync(user, RoleNames.Chef), "UnlinkUserFailed");
            }
        }

        // refresh token silinir ve security stamp yenilenir; mevcut access token'lar anında geçersiz olur (Program.cs -> OnTokenValidated).
        // değişiklikler, sonrasında çağrılan UserManager işlemi (AddToRoleAsync / RemoveFromRoleAsync) ile kaydedilir.
        private static void RevokeSessions(AppUser user)
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            user.SecurityStamp = Guid.NewGuid().ToString("N");
        }

        private static void EnsureSucceeded(IdentityResult result, string propertyName)
        {
            if (!result.Succeeded)
                throw new LogicException(propertyName, string.Join(" | ", result.Errors.Select(e => e.Description)));
        }

        #region Dosya İşlemleri
        private async Task<string> SaveFileAsync(IFormFile file)
        {
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "chefs");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return $"/images/chefs/{uniqueFileName}";
        }

        private void DeleteFile(string? imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return;
            var filePath = Path.Combine(_environment.WebRootPath, imageUrl.TrimStart('/'));

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        #endregion
    }
}
