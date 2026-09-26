using System.Threading;
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Yummy.Core.DTOs.ProductDTOs;
using Yummy.Core.DTOs.ReservationDTOs;
using Yummy.Core.Exceptions;
using Yummy.Core.IRepositories;
using Yummy.Core.IUnitOfWork;
using Yummy.Core.Services;
using Yummy.Entity;
using Yummy.Entity.Enums;

namespace Yummy.Business.Managers
{
    public class ReservationManager : IReservationService
    {
        private readonly IGenericRepository<Reservation> _reservationRepository;
        private readonly IGenericRepository<DiningTable> _tableRepository;
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailService;
        private readonly ILogger<ReservationManager> _logger;

        // kullanıcı, rezervasyon saatine bu süreden az kaldığında rezervasyonunu iptal edemez veya güncelleyemez.
        private const int MinHoursBeforeChange = 2;

        public ReservationManager(IGenericRepository<Reservation> reservationRepository, IGenericRepository<DiningTable> tableRepository, IUnitOfWork uow, IMapper mapper, IEmailService emailService, ILogger<ReservationManager> logger)
        {
            _reservationRepository = reservationRepository;
            _tableRepository = tableRepository;
            _uow = uow;
            _mapper = mapper;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task AddReservationAsync(string userId, ReservationCreateDto dto, CancellationToken cancellationToken = default)
        {
            var reservation = _mapper.Map<Reservation>(dto);

            if (!Guid.TryParse(userId, out Guid parsedUserId))
                throw new LogicException("InvalidUserId", "Kullanıcı kimliği geçersiz veya doğrulanamadı.");

            reservation.AppUserId = parsedUserId;

            var targetDate = dto.ReservationDate.Date;
            if (!TimeSpan.TryParse(dto.ReservationTime, out TimeSpan reqStart) || !TimeSpan.TryParse(dto.ReservationEndTime, out TimeSpan reqEnd))
                throw new LogicException("InvalidTime", "Geçersiz saat formatı.");
                
            var newReservationDateTime = targetDate.Add(reqStart);
            if (newReservationDateTime < DateTime.Now)
                throw new LogicException("PastReservation", "Geçmiş bir tarihe veya saate rezervasyon yapılamaz.");

            DiningTable selectedTable = null!;

            // müsaitlik kontrolü ile kayıt aynı kilit altında yapılır; aksi halde eşzamanlı iki istek aynı masayı boş görüp ikisi de kaydedebilir.
            await _uow.ExecuteInLockedTransactionAsync(GetDateLockKey(targetDate), async () =>
            {
                var availableTables = await GetAvailableTablesAsync(targetDate, reqStart, reqEnd, dto.NumberOfGuests, null, cancellationToken);

                if (dto.SelectedTableId.HasValue)
                {
                    selectedTable = availableTables.FirstOrDefault(t => t.DiningTableId == dto.SelectedTableId.Value)
                        ?? throw new LogicException("TableNotAvailable", "Seçtiğiniz masa istenilen saat aralığında uygun değil veya kapasitesi yetersiz.");
                }
                else
                {
                    selectedTable = availableTables.FirstOrDefault()
                        ?? throw new LogicException("NoTable", "Seçtiğiniz tarih ve saat aralığında kişi sayınıza uygun boş masamız bulunmamaktadır.");
                }

                reservation.DiningTableId = selectedTable.DiningTableId;

                await _reservationRepository.AddAsync(reservation, cancellationToken);
                await _uow.SaveAsync(cancellationToken);
            }, cancellationToken);

            await TrySendEmailAsync(reservation, "ReservationReceivedTemplate.html", "Yummy Restoran - Rezervasyon Talebiniz Alındı", new Dictionary<string, string>
            {
                ["{{Name}}"] = reservation.Name,
                ["{{Surname}}"] = reservation.Surname,
                ["{{Date}}"] = reservation.ReservationDate.ToString("dd.MM.yyyy"),
                ["{{Time}}"] = reservation.ReservationTime,
                ["{{Guests}}"] = reservation.NumberOfGuests.ToString(),
                ["{{Phone}}"] = reservation.Phone,
                ["{{TableNo}}"] = selectedTable.TableNo,
                ["{{Location}}"] = selectedTable.Location ?? "Belirtilmemiş"
            });
        }

        public async Task CancelReservationAsync(string userId, Guid reservationId, CancellationToken cancellationToken = default)
        {
            var reservation = await _reservationRepository.GetByIdAsync(reservationId, cancellationToken);
            if (reservation == null)
                throw new LogicException("NotFound", "Rezervasyon bulunamadı.");

            if (reservation.AppUserId.ToString() != userId)
                throw new LogicException("Forbidden", "Bu işlem için yetkiniz yok.");

            if (reservation.ReservationStatus == ReservationStatus.Cancelled)
                throw new LogicException("AlreadyCancelled", "Bu rezervasyon zaten daha önce iptal edilmiş.");

            if (reservation.ReservationStatus == ReservationStatus.Completed)
                throw new LogicException("NotAllowed", "Tamamlanmış rezervasyonlar iptal edilemez.");

            if (!TimeSpan.TryParse(reservation.ReservationTime, out TimeSpan reservationTime))
                throw new LogicException("InvalidTime", "Rezervasyonun saat bilgisi geçersiz.");

            // saat ve dakika birlikte dikkate alınır (19:59'luk bir rezervasyon 19:00 gibi hesaplanmaz).
            var reservationDateTime = reservation.ReservationDate.Date.Add(reservationTime);
            if (reservationDateTime <= DateTime.Now.AddHours(MinHoursBeforeChange))
                throw new LogicException("TooLate", $"Rezervasyon saatinize {MinHoursBeforeChange} saatten az kaldığı için iptal işlemi yapılamaz.");

            reservation.ReservationStatus = ReservationStatus.Cancelled;

            _reservationRepository.Update(reservation);
            await _uow.SaveAsync(cancellationToken);

            var table = await _tableRepository.GetByIdAsync(reservation.DiningTableId, cancellationToken);
            await TrySendEmailAsync(reservation, "ReservationCancelledTemplate.html", "Yummy Restoran - Rezervasyonunuz İptal Edildi", new Dictionary<string, string>
            {
                ["{{Name}}"] = reservation.Name,
                ["{{Surname}}"] = reservation.Surname,
                ["{{Date}}"] = reservation.ReservationDate.ToString("dd.MM.yyyy"),
                ["{{Time}}"] = reservation.ReservationTime,
                ["{{Guests}}"] = reservation.NumberOfGuests.ToString(),
                ["{{TableNo}}"] = table?.TableNo ?? "",
                ["{{Location}}"] = table?.Location ?? "Belirtilmemiş"
            });
        }

        public async Task<CheckAvailabilityResponseDto> CheckAvailabilityAsync(CheckAvailabilityRequestDto dto, CancellationToken cancellationToken = default)
        { 
            var targetDate = dto.ReservationDate.Date;
            
            if (!TimeSpan.TryParse(dto.ReservationTime, out TimeSpan reqStart) || !TimeSpan.TryParse(dto.ReservationEndTime, out TimeSpan reqEnd))
                throw new LogicException("InvalidTime", "Geçersiz saat formatı.");

            var availableTables = await GetAvailableTablesAsync(targetDate, reqStart, reqEnd, dto.NumberOfGuests, null, cancellationToken);

            var response = new CheckAvailabilityResponseDto
            {
                ReservationDate = targetDate,
                IsFullyBooked = !availableTables.Any(),
                AvailableTables = availableTables.Select(t => new AvailableTableDto
                {
                    DiningTableId = t.DiningTableId,
                    TableNo = t.TableNo,
                    Capacity = t.Capacity
                }).ToList()
            };

            return response;
        }

        public async Task<IEnumerable<ReservationListDto>> GetAllReservationsAsync(CancellationToken cancellationToken = default)
        {
            var entities = await _reservationRepository.GetAllAsync(cancellationToken, x => x.DiningTable);
            return _mapper.Map<IEnumerable<ReservationListDto>>(entities);
        }

        public async Task<ReservationListDto> GetReservationByIdAsync(Guid reservationId, CancellationToken cancellationToken = default)
        {
            var entities = await _reservationRepository.GetWhereAsync(x => x.ReservationId == reservationId, cancellationToken, x => x.DiningTable);
            var reservation = _mapper.Map<IEnumerable<ReservationListDto>>(entities).FirstOrDefault();

            return reservation ?? throw new LogicException("NotFound", "Rezervasyon bulunamadı.");
        }

        public async Task<IEnumerable<ReservationListDto>> GetTodaysReservationListAsync(CancellationToken cancellationToken = default)
        {
            // aralık sorgusu: kayıtta saat kısmı olsa bile bugünün rezervasyonları kaçırılmaz ve ReservationDate üzerindeki index kullanılabilir.
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var entities = await _reservationRepository.GetWhereAsync(x => x.ReservationDate >= today && x.ReservationDate < tomorrow, cancellationToken, x => x.DiningTable);
            return _mapper.Map<IEnumerable<ReservationListDto>>(entities);
        }

        public async Task<PastReservationByUserDto> GetUserReservationByIdAsync(string userId, Guid reservationId, CancellationToken cancellationToken = default)
        {
            if (!Guid.TryParse(userId, out Guid parsedUserId))
                throw new LogicException("InvalidUserId", "Kullanıcı kimliği geçersiz.");

            var entities = await _reservationRepository.GetWhereAsync(x => x.ReservationId == reservationId && x.AppUserId == parsedUserId, cancellationToken, x => x.DiningTable);
            var reservation = _mapper.Map<IEnumerable<PastReservationByUserDto>>(entities).FirstOrDefault();

            return reservation ?? throw new LogicException("NotFound", "Rezervasyon bulunamadı.");
        }

        public async Task<IEnumerable<PastReservationByUserDto>> SeeMyPastReservationsAsync(string userId, CancellationToken cancellationToken = default)
        {
            if (!Guid.TryParse(userId, out Guid parsedUserId))
                throw new LogicException("InvalidUserId", "Kullanıcı kimliği geçersiz.");

            var entities = await _reservationRepository.GetWhereAsync(x => x.AppUserId == parsedUserId, cancellationToken, x => x.DiningTable);
            var sortedEntities = entities.OrderByDescending(x => x.ReservationDate);
            return _mapper.Map<IEnumerable<PastReservationByUserDto>>(sortedEntities);
        }

        public async Task UpdateReservationAsync(string userId, ReservationUpdateDto dto, CancellationToken cancellationToken = default)
        {
            if (!Guid.TryParse(userId, out Guid parsedUserId))
                throw new LogicException("InvalidUserId", "Kullanıcı kimliği geçersiz.");

            var reservation = await _reservationRepository.GetSingleAsync(x => x.ReservationId == dto.ReservationId && x.AppUserId == parsedUserId);
            if (reservation == null)
                throw new LogicException("InvalidReservationId", "Güncellenmek istenen rezarvasyon kimliği bulunamadı.");

            if (reservation.ReservationStatus == ReservationStatus.Completed || reservation.ReservationStatus == ReservationStatus.Cancelled)
                throw new LogicException("NotAllowed", "Tamamlanmış veya iptal edilmiş rezervasyonlar üzerinde güncelleme yapılamaz.");

            var exactReservationDateTime = reservation.ReservationDate.Date;
            if (TimeSpan.TryParse(reservation.ReservationTime, out TimeSpan parsedTime))
                exactReservationDateTime = exactReservationDateTime.Add(parsedTime);

            if (exactReservationDateTime < DateTime.Now)
                throw new LogicException("PastReservation", "Geçmiş rezervasyonlarda herhangi bir değişiklik yapılamaz.");

            if (exactReservationDateTime <= DateTime.Now.AddHours(MinHoursBeforeChange))
                throw new LogicException("TooLate", $"Rezervasyonunuza {MinHoursBeforeChange} saatten az bir süre kaldığı için değişiklik yapılamaz.");

            bool isChanged = false;

            string incomingMessage = dto.Message ?? string.Empty;

            var targetDate = dto.ReservationDate.Date;
            TimeSpan reqStart = default, reqEnd = default;

            bool isSlotChanged = reservation.ReservationDate.Date != dto.ReservationDate.Date ||
                reservation.ReservationTime != dto.ReservationTime ||
                reservation.ReservationEndTime != dto.ReservationEndTime ||
                reservation.NumberOfGuests != dto.NumberOfGuests;

            if (isSlotChanged)
            {
                if (!TimeSpan.TryParse(dto.ReservationTime, out reqStart) || !TimeSpan.TryParse(dto.ReservationEndTime, out reqEnd))
                    throw new LogicException("InvalidTime", "Geçersiz saat formatı.");

                var newReservationDateTime = targetDate.Add(reqStart);
                if (newReservationDateTime < DateTime.Now)
                    throw new LogicException("PastReservation", "Geçmiş bir tarihe veya saate rezervasyon güncellenemez.");
            }

            await _uow.ExecuteInLockedTransactionAsync(GetDateLockKey(targetDate), async () =>
            {
                if (isSlotChanged)
                {
                    var availableTables = await GetAvailableTablesAsync(targetDate, reqStart, reqEnd, dto.NumberOfGuests, reservation.ReservationId, cancellationToken);

                    var selectedTable = availableTables.FirstOrDefault()
                        ?? throw new LogicException("NoTable", "Seçtiğiniz yeni tarih ve saat aralığında kişi sayınıza uygun boş masamız bulunmamaktadır.");

                    reservation.DiningTableId = selectedTable.DiningTableId;
                    isChanged = true;
                }

                if (reservation.Message != incomingMessage)
                    isChanged = true;

                if (isChanged && reservation.ReservationStatus == ReservationStatus.Approved)
                    reservation.ReservationStatus = ReservationStatus.Pending;
                _mapper.Map(dto, reservation);

                _reservationRepository.Update(reservation);
                await _uow.SaveAsync(cancellationToken);
            }, cancellationToken);

            var table = await _tableRepository.GetByIdAsync(reservation.DiningTableId, cancellationToken);
            await TrySendEmailAsync(reservation, "ReservationUpdatedTemplate.html", "Yummy Restoran - Rezervasyonunuz Güncellendi ve Onay Bekliyor", new Dictionary<string, string>
            {
                ["{{Name}}"] = reservation.Name,
                ["{{Surname}}"] = reservation.Surname,
                ["{{NewDate}}"] = reservation.ReservationDate.ToString("dd.MM.yyyy"),
                ["{{NewTime}}"] = reservation.ReservationTime,
                ["{{NewGuests}}"] = reservation.NumberOfGuests.ToString(),
                ["{{TableNo}}"] = table?.TableNo ?? "",
                ["{{Location}}"] = table?.Location ?? "Belirtilmemiş"
            });
        }

        public async Task UpdateReservationStatusAsync(UpdateReservationDto dto, CancellationToken cancellationToken = default)
        {
            var reservation = await _reservationRepository.GetByIdAsync(dto.ReservationId, cancellationToken);
            if (reservation == null)
                throw new LogicException("NotFound", "Rezervasyon bulunamadı.");

            if (reservation.ReservationStatus == dto.ReservationStatus)
                throw new LogicException("SameStatus", "Rezervasyon zaten bu durumda.");

            if (reservation.ReservationStatus == ReservationStatus.Completed)
                throw new LogicException("NotAllowed", "Tamamlanmış rezervasyonların durumu değiştirilemez.");

            // iptal edilmiş bir rezervasyon tekrar aktif edilirken masası bu arada başka bir rezervasyona verilmiş olabilir.
            // bu nedenle masanın hâlâ uygun olduğu, kayıt ile aynı kilit altında tekrar kontrol edilir.
            bool isReactivating = reservation.ReservationStatus == ReservationStatus.Cancelled &&
                (dto.ReservationStatus == ReservationStatus.Pending || dto.ReservationStatus == ReservationStatus.Approved);

            if (isReactivating)
            {
                if (!TimeSpan.TryParse(reservation.ReservationTime, out TimeSpan resStart) || !TimeSpan.TryParse(reservation.ReservationEndTime, out TimeSpan resEnd))
                    throw new LogicException("InvalidTime", "Rezervasyonun saat bilgisi geçersiz.");

                var targetDate = reservation.ReservationDate.Date;
                if (targetDate.Add(resStart) < DateTime.Now)
                    throw new LogicException("PastReservation", "Başlangıç saati geçmiş bir rezervasyon tekrar aktif edilemez.");

                await _uow.ExecuteInLockedTransactionAsync(GetDateLockKey(targetDate), async () =>
                {
                    var table = await _tableRepository.GetByIdAsync(reservation.DiningTableId, cancellationToken);
                    if (table == null || !table.IsActive)
                        throw new LogicException("TableNotAvailable", "Rezervasyonun masası artık kullanımda değil. Kullanıcının yeni bir rezervasyon oluşturması gerekir.");

                    var busyTableIds = await GetBusyTableIdsAsync(targetDate, resStart, resEnd, reservation.ReservationId, cancellationToken);
                    if (busyTableIds.Contains(reservation.DiningTableId))
                        throw new LogicException("TableNotAvailable", "Rezervasyonun masası bu saat aralığında başka bir rezervasyona ayrılmış. Kullanıcının yeni bir rezervasyon oluşturması gerekir.");

                    reservation.ReservationStatus = dto.ReservationStatus;
                    _reservationRepository.Update(reservation);
                    await _uow.SaveAsync(cancellationToken);
                }, cancellationToken);
            }
            else
            {
                reservation.ReservationStatus = dto.ReservationStatus;
                _reservationRepository.Update(reservation);
                await _uow.SaveAsync(cancellationToken);
            }

            string statusTitle = "";
            string statusMessage = "";
            string statusColor = "";

            switch (reservation.ReservationStatus)
            {
                case ReservationStatus.Approved:
                    statusTitle = "Rezervasyon Onaylandı";
                    statusMessage = "onaylanmıştır. Sizi ağırlamaktan mutluluk duyacağız";
                    statusColor = "#28a745";
                    break;
                case ReservationStatus.Cancelled:
                    statusTitle = "Rezervasyon İptal Edildi";
                    statusMessage = "operasyonel nedenler nedeniyle iptal edilmiştir";
                    statusColor = "#dc3545";
                    break;
                case ReservationStatus.Pending:
                    statusTitle = "Rezervasyon Beklemede";
                    statusMessage = "tekrar değerlendirmeye alınmış ve bekleme durumuna çekilmiştir";
                    statusColor = "#ffc107";
                    break;
                case ReservationStatus.Completed:
                default:
                    return;
            }

            var table = await _tableRepository.GetByIdAsync(reservation.DiningTableId, cancellationToken);
            await TrySendEmailAsync(reservation, "ReservationStatusTemplate.html", $"Yummy Restoran - Rezervasyon Bilgilendirmesi ({statusTitle})", new Dictionary<string, string>
            {
                ["{{Name}}"] = reservation.Name,
                ["{{Surname}}"] = reservation.Surname,
                ["{{StatusTitle}}"] = statusTitle,
                ["{{StatusMessage}}"] = statusMessage,
                ["#112233"] = statusColor,
                ["{{Date}}"] = reservation.ReservationDate.ToString("dd.MM.yyyy"),
                ["{{Time}}"] = reservation.ReservationTime,
                ["{{Guests}}"] = reservation.NumberOfGuests.ToString(),
                ["{{TableNo}}"] = table?.TableNo ?? "",
                ["{{Location}}"] = table?.Location ?? "Belirtilmemiş"
            });
        }

        public async Task<IEnumerable<TableStatusForMapDto>> GetTableStatusesForMapAsync(DateTime date, string time, string endTime, CancellationToken cancellationToken = default)
        {
            var targetDate = date.Date;

            if (!TimeSpan.TryParse(time, out TimeSpan reqStart) || !TimeSpan.TryParse(endTime, out TimeSpan reqEnd))
                throw new LogicException("InvalidTime", "Geçersiz saat formatı.");

            var busyTableIds = await GetBusyTableIdsAsync(targetDate, reqStart, reqEnd, null, cancellationToken);
            var tables = await _tableRepository.GetWhereAsync(t => t.IsActive, cancellationToken);

            return tables.Select(table => new TableStatusForMapDto
            {
                DiningTableId = table.DiningTableId,
                TableNo = table.TableNo,
                Capacity = table.Capacity,
                Location = table.Location,
                IsAvailable = !busyTableIds.Contains(table.DiningTableId)
            }).ToList();
        }

        // e-posta, rezervasyon veritabanına kaydedildikten sonra gönderilir. şablon bulunamaz veya SMTP hata verirse işlem geri alınmaz;
        // hata loglanır ve istemciye başarılı cevap dönülür. aksi halde kullanıcı hata görüp tekrar dener ve mükerrer rezervasyon oluşur.
        private async Task TrySendEmailAsync(Reservation reservation, string templateName, string subject, IReadOnlyDictionary<string, string> placeholders)
        {
            try
            {
                var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", templateName);
                var mailBody = await File.ReadAllTextAsync(templatePath);

                foreach (var placeholder in placeholders)
                    mailBody = mailBody.Replace(placeholder.Key, placeholder.Value);

                await _emailService.SendEmailAsync(reservation.Email, subject, mailBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rezervasyon e-postası gönderilemedi. Rezervasyon: {ReservationId}, Şablon: {TemplateName}", reservation.ReservationId, templateName);
            }
        }

        // aynı güne ait rezervasyon yazma işlemleri bu anahtar ile kilitlenir. farklı günlerin istekleri birbirini beklemez.
        private static string GetDateLockKey(DateTime date) => $"reservation:{date:yyyy-MM-dd}";

        // iki saat aralığı, biri diğeri bitmeden başlıyorsa çakışır. uç uca eklenen aralıklar (19:00-21:00 ve 21:00-22:00) çakışmaz.
        private static bool IsOverlapping(TimeSpan startA, TimeSpan endA, TimeSpan startB, TimeSpan endB) =>
            startA < endB && endA > startB;

        // verilen gün ve saat aralığında aktif (Pending/Approved) bir rezervasyonu bulunan masaların id'lerini döner.
        // excludeReservationId: güncellenen rezervasyonun kendi masasını dolu saymaması için hariç tutulur.
        private async Task<HashSet<Guid>> GetBusyTableIdsAsync(DateTime date, TimeSpan start, TimeSpan end, Guid? excludeReservationId, CancellationToken cancellationToken)
        {
            var targetDate = date.Date;
            var excludedId = excludeReservationId ?? Guid.Empty;

            var activeReservations = await _reservationRepository.GetWhereAsync(r => r.ReservationDate.Date == targetDate && r.ReservationId != excludedId &&
                           (r.ReservationStatus == ReservationStatus.Approved || r.ReservationStatus == ReservationStatus.Pending), cancellationToken);

            return activeReservations
                .Where(r => TimeSpan.TryParse(r.ReservationTime, out TimeSpan resStart) &&
                            TimeSpan.TryParse(r.ReservationEndTime, out TimeSpan resEnd) &&
                            IsOverlapping(start, end, resStart, resEnd))
                .Select(r => r.DiningTableId)
                .ToHashSet();
        }

        // kişi sayısına yeten, aktif ve verilen saat aralığında boş olan masaları kapasiteye göre küçükten büyüğe sıralı döner.
        private async Task<List<DiningTable>> GetAvailableTablesAsync(DateTime date, TimeSpan start, TimeSpan end, int numberOfGuests, Guid? excludeReservationId, CancellationToken cancellationToken)
        {
            var busyTableIds = await GetBusyTableIdsAsync(date, start, end, excludeReservationId, cancellationToken);
            var tables = await _tableRepository.GetWhereAsync(t => t.IsActive && t.Capacity >= numberOfGuests, cancellationToken);

            return tables
                .Where(t => !busyTableIds.Contains(t.DiningTableId))
                .OrderBy(t => t.Capacity)
                .ToList();
        }
    }
}
