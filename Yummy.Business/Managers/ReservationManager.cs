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
            
            var activeReservations = await _reservationRepository.GetWhereAsync(r => r.ReservationDate.Date == targetDate &&
                           (r.ReservationStatus == ReservationStatus.Approved || r.ReservationStatus == ReservationStatus.Pending), cancellationToken);

            var tables = await _tableRepository.GetWhereAsync(t => t.IsActive && t.Capacity >= dto.NumberOfGuests, cancellationToken);
            var availableTables = tables.OrderBy(t => t.Capacity).ToList();
            
            foreach(var res in activeReservations)
            {
                if (TimeSpan.TryParse(res.ReservationTime, out TimeSpan resStart) && TimeSpan.TryParse(res.ReservationEndTime, out TimeSpan resEnd))
                {
                    if ((reqStart >= resStart && reqStart < resEnd) || (reqEnd > resStart && reqEnd <= resEnd) || (reqStart <= resStart && reqEnd >= resEnd))
                    {
                        var tableToRemove = availableTables.FirstOrDefault(t => t.DiningTableId == res.DiningTableId);
                        if (tableToRemove != null)
                            availableTables.Remove(tableToRemove);
                    }
                }
            }
            
            DiningTable? selectedTable = null;
            if (dto.SelectedTableId.HasValue)
            {
                selectedTable = availableTables.FirstOrDefault(t => t.DiningTableId == dto.SelectedTableId.Value);
                if (selectedTable == null)
                    throw new LogicException("TableNotAvailable", "Seçtiğiniz masa istenilen saat aralığında uygun değil veya kapasitesi yetersiz.");
            }
            else
            {
                selectedTable = availableTables.FirstOrDefault();
                if (selectedTable == null)
                    throw new LogicException("NoTable", "Seçtiğiniz tarih ve saat aralığında kişi sayınıza uygun boş masamız bulunmamaktadır.");
            }
                
            reservation.DiningTableId = selectedTable.DiningTableId;

            await _reservationRepository.AddAsync(reservation, cancellationToken);
            await _uow.SaveAsync(cancellationToken);

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
                
            var activeReservations = await _reservationRepository.GetWhereAsync(r => r.ReservationDate.Date == targetDate &&
                           (r.ReservationStatus == ReservationStatus.Approved || r.ReservationStatus == ReservationStatus.Pending), cancellationToken);

            var tables = await _tableRepository.GetWhereAsync(t => t.IsActive && t.Capacity >= dto.NumberOfGuests, cancellationToken);
            var availableTables = tables.OrderBy(t => t.Capacity).ToList();
            
            foreach(var res in activeReservations)
            {
                if (TimeSpan.TryParse(res.ReservationTime, out TimeSpan resStart) && TimeSpan.TryParse(res.ReservationEndTime, out TimeSpan resEnd))
                {
                    if ((reqStart >= resStart && reqStart < resEnd) || (reqEnd > resStart && reqEnd <= resEnd) || (reqStart <= resStart && reqEnd >= resEnd))
                    {
                        var tableToRemove = availableTables.FirstOrDefault(t => t.DiningTableId == res.DiningTableId);
                        if (tableToRemove != null)
                            availableTables.Remove(tableToRemove);
                    }
                }
            }

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

            if (reservation.ReservationDate.Date != dto.ReservationDate.Date ||
                reservation.ReservationTime != dto.ReservationTime ||
                reservation.ReservationEndTime != dto.ReservationEndTime ||
                reservation.NumberOfGuests != dto.NumberOfGuests)
            {
                var targetDate = dto.ReservationDate.Date;
                if (!TimeSpan.TryParse(dto.ReservationTime, out TimeSpan reqStart) || !TimeSpan.TryParse(dto.ReservationEndTime, out TimeSpan reqEnd))
                    throw new LogicException("InvalidTime", "Geçersiz saat formatı.");
                    
                var activeReservations = await _reservationRepository.GetWhereAsync(r => r.ReservationDate.Date == targetDate && r.ReservationId != reservation.ReservationId &&
                               (r.ReservationStatus == ReservationStatus.Approved || r.ReservationStatus == ReservationStatus.Pending), cancellationToken);

                var tables = await _tableRepository.GetWhereAsync(t => t.IsActive && t.Capacity >= dto.NumberOfGuests, cancellationToken);
                var availableTables = tables.OrderBy(t => t.Capacity).ToList();
                
                foreach(var res in activeReservations)
                {
                    if (TimeSpan.TryParse(res.ReservationTime, out TimeSpan resStart) && TimeSpan.TryParse(res.ReservationEndTime, out TimeSpan resEnd))
                    {
                        if ((reqStart >= resStart && reqStart < resEnd) || (reqEnd > resStart && reqEnd <= resEnd) || (reqStart <= resStart && reqEnd >= resEnd))
                        {
                            var tableToRemove = availableTables.FirstOrDefault(t => t.DiningTableId == res.DiningTableId);
                            if (tableToRemove != null)
                                availableTables.Remove(tableToRemove);
                        }
                    }
                }
                
                var selectedTable = availableTables.FirstOrDefault();
                if (selectedTable == null)
                    throw new LogicException("NoTable", "Seçtiğiniz yeni tarih ve saat aralığında kişi sayınıza uygun boş masamız bulunmamaktadır.");
                    
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

            reservation.ReservationStatus = dto.ReservationStatus;
            _reservationRepository.Update(reservation);
            await _uow.SaveAsync(cancellationToken);

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
                
            var activeReservations = await _reservationRepository.GetWhereAsync(r => r.ReservationDate.Date == targetDate &&
                           (r.ReservationStatus == ReservationStatus.Approved || r.ReservationStatus == ReservationStatus.Pending), cancellationToken);

            var tables = await _tableRepository.GetWhereAsync(t => t.IsActive, cancellationToken);
            
            var statuses = new List<TableStatusForMapDto>();
            
            foreach (var table in tables)
            {
                bool isAvailable = true;
                
                var tableReservations = activeReservations.Where(r => r.DiningTableId == table.DiningTableId);
                foreach (var res in tableReservations)
                {
                    if (TimeSpan.TryParse(res.ReservationTime, out TimeSpan resStart) && TimeSpan.TryParse(res.ReservationEndTime, out TimeSpan resEnd))
                    {
                        if ((reqStart >= resStart && reqStart < resEnd) || (reqEnd > resStart && reqEnd <= resEnd) || (reqStart <= resStart && reqEnd >= resEnd))
                        {
                            isAvailable = false;
                            break;
                        }
                    }
                }
                
                statuses.Add(new TableStatusForMapDto
                {
                    DiningTableId = table.DiningTableId,
                    TableNo = table.TableNo,
                    Capacity = table.Capacity,
                    Location = table.Location,
                    IsAvailable = isAvailable
                });
            }
            
            return statuses;
        }
    }
}
