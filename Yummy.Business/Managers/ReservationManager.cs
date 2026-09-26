using System.Threading;
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
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

        public ReservationManager(IGenericRepository<Reservation> reservationRepository, IGenericRepository<DiningTable> tableRepository, IUnitOfWork uow, IMapper mapper, IEmailService emailService)
        {
            _reservationRepository = reservationRepository;
            _tableRepository = tableRepository;
            _uow = uow;
            _mapper = mapper;
            _emailService = emailService;
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

            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "ReservationReceivedTemplate.html");
            if (!File.Exists(templatePath))
                throw new LogicException("TemplateError", "E-posta şablonu bulunamadı.");

            var emailTemplate = await File.ReadAllTextAsync(templatePath);
            var mailBody = emailTemplate
                .Replace("{{Name}}", dto.Name)
                .Replace("{{Surname}}", dto.Surname)
                .Replace("{{Date}}", dto.ReservationDate.ToString("dd.MM.yyyy"))
                .Replace("{{Time}}", dto.ReservationTime)
                .Replace("{{Guests}}", dto.NumberOfGuests.ToString())
                .Replace("{{Phone}}", dto.Phone)
                .Replace("{{TableNo}}", selectedTable.TableNo)
                .Replace("{{Location}}", selectedTable.Location ?? "Belirtilmemiş");

            var subject = "Yummy Restoran - Rezervasyon Talebiniz Alındı";

            await _emailService.SendEmailAsync(dto.Email, subject, mailBody);
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

            var reservationDateTime = reservation.ReservationDate.Date.AddHours(int.Parse(reservation.ReservationTime.Split(':')[0]));

            if (reservationDateTime < DateTime.Now.AddHours(1))
                throw new LogicException("TooLate", "Rezervasyon saatinize 2 saatten az kaldığı için iptal işlemi yapılamaz.");

            reservation.ReservationStatus = ReservationStatus.Cancelled;

            _reservationRepository.Update(reservation);
            await _uow.SaveAsync(cancellationToken);

            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "ReservationCancelledTemplate.html");
            var emailTemplate = await File.ReadAllTextAsync(templatePath);

            var table = await _tableRepository.GetByIdAsync(reservation.DiningTableId, cancellationToken);
            var mailBody = emailTemplate
                .Replace("{{Name}}", reservation.Name)
                .Replace("{{Surname}}", reservation.Surname)
                .Replace("{{Date}}", reservation.ReservationDate.ToString("dd.MM.yyyy"))
                .Replace("{{Time}}", reservation.ReservationTime)
                .Replace("{{Guests}}", reservation.NumberOfGuests.ToString())
                .Replace("{{TableNo}}", table?.TableNo ?? "")
                .Replace("{{Location}}", table?.Location ?? "Belirtilmemiş");

            await _emailService.SendEmailAsync(reservation.Email, "Yummy Restoran - Rezervasyonunuz İptal Edildi", mailBody);
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
            var entities = await _reservationRepository.GetWhereAsync(x=>x.ReservationDate == DateTime.Today, cancellationToken, x => x.DiningTable);
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

            if (exactReservationDateTime <= DateTime.Now.AddHours(2))
                throw new LogicException("TooLate", "Rezervasyonunuza 2 saatten az bir süre kaldığı için değişiklik yapılamaz.");

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

            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "ReservationUpdatedTemplate.html");

            if (!File.Exists(templatePath))
                throw new LogicException("TemplateError", "Güncelleme e-posta şablonu bulunamadı.");

            var emailTemplate = await File.ReadAllTextAsync(templatePath);

            var table = await _tableRepository.GetByIdAsync(reservation.DiningTableId, cancellationToken);
            var mailBody = emailTemplate
                .Replace("{{Name}}", reservation.Name)
                .Replace("{{Surname}}", reservation.Surname)
                .Replace("{{NewDate}}", reservation.ReservationDate.ToString("dd.MM.yyyy"))
                .Replace("{{NewTime}}", reservation.ReservationTime)
                .Replace("{{NewGuests}}", reservation.NumberOfGuests.ToString())
                .Replace("{{TableNo}}", table?.TableNo ?? "")
                .Replace("{{Location}}", table?.Location ?? "Belirtilmemiş");

            var subject = "Yummy Restoran - Rezervasyonunuz Güncellendi ve Onay Bekliyor";

            await _emailService.SendEmailAsync(reservation.Email, subject, mailBody);
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

            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "ReservationStatusTemplate.html");
            if (!File.Exists(templatePath))
                throw new LogicException("TemplateError", "Durum güncelleme e-posta şablonu bulunamadı.");

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

            var emailTemplate = await File.ReadAllTextAsync(templatePath);

            var table = await _tableRepository.GetByIdAsync(reservation.DiningTableId, cancellationToken);
            var mailBody = emailTemplate
                .Replace("{{Name}}", reservation.Name)
                .Replace("{{Surname}}", reservation.Surname)
                .Replace("{{StatusTitle}}", statusTitle)
                .Replace("{{StatusMessage}}", statusMessage)
                .Replace("#112233", statusColor)
                .Replace("{{Date}}", reservation.ReservationDate.ToString("dd.MM.yyyy"))
                .Replace("{{Time}}", reservation.ReservationTime)
                .Replace("{{Guests}}", reservation.NumberOfGuests.ToString())
                .Replace("{{TableNo}}", table?.TableNo ?? "")
                .Replace("{{Location}}", table?.Location ?? "Belirtilmemiş");

            var subject = $"Yummy Restoran - Rezervasyon Bilgilendirmesi ({statusTitle})";

            await _emailService.SendEmailAsync(reservation.Email, subject, mailBody);
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
