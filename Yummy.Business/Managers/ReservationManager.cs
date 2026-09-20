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
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailService;

        public ReservationManager(IGenericRepository<Reservation> reservationRepository, IUnitOfWork uow, IMapper mapper, IEmailService emailService)
        {
            _reservationRepository = reservationRepository;
            _uow = uow;
            _mapper = mapper;
            _emailService = emailService;
        }

        public async Task AddReservationAsync(string userId, ReservationCreateDto dto, CancellationToken cancellationToken = default)
        {
            var reservation = _mapper.Map<Reservation>(dto);

            if (!Guid.TryParse(userId, out Guid parsedUserId))
                throw new LogicException("InvalidUserId", "Kullanıcı kimliği geçersiz veya doğrulanamadı.");

            reservation.AppUserId = parsedUserId; // claimden çekilen userId, reservation entitysi içerisinde bulunan AppUserId propuna atanır. 

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
                .Replace("{{Phone}}", dto.Phone);

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

            var mailBody = emailTemplate
                .Replace("{{Name}}", reservation.Name)
                .Replace("{{Surname}}", reservation.Surname)
                .Replace("{{Date}}", reservation.ReservationDate.ToString("dd.MM.yyyy"))
                .Replace("{{Time}}", reservation.ReservationTime)
                .Replace("{{Guests}}", reservation.NumberOfGuests.ToString());

            await _emailService.SendEmailAsync(reservation.Email, "Yummy Restoran - Rezervasyonunuz İptal Edildi", mailBody);
        }

        public async Task<CheckAvailabilityResponseDto> CheckAvailabilityAsync(CheckAvailabilityRequestDto dto, CancellationToken cancellationToken = default)
        { 
            int maxTables = 10; // restoranda bulunan masa sayısı
            var reservationDuration = TimeSpan.FromHours(2); // bir rezervasyonun süresi

            var allTimeSlots = new List<string>
            {
                "09:00", "09:30", "10:00", "10:30", "11:00", "11:30",
                "12:00", "12:30", "13:00", "13:30", "14:00", "14:30",
                "15:00", "15:30", "16:00", "16:30", "17:00", "17:30",
                "18:00", "18:30", "19:00", "19:30", "20:00", "20:30",
                "21:00"
            };

            var targetDate = dto.ReservationDate.Date;
            var now = DateTime.Now;

            // rezarvasyonun tarihinde olan rezarvasyonları, onaylanmış ve bekliyor olan rezarvasyonları çeker.
            var activeReservations = await _reservationRepository.GetWhereAsync(r => r.ReservationDate.Date == targetDate &&
                           (r.ReservationStatus == ReservationStatus.Approved || r.ReservationStatus == ReservationStatus.Pending), cancellationToken);

            var existingIntervals = activeReservations
                .Select(r =>
                {
                    TimeSpan.TryParse(r.ReservationTime, out TimeSpan start);
                    return new { Start = start, End = start.Add(reservationDuration) };
                }).ToList();

            var response = new CheckAvailabilityResponseDto
            {
                ReservationDate = targetDate,
                AvailableTimeSlots = new List<string>()
            };

            foreach (var slot in allTimeSlots)
            {
                if (!TimeSpan.TryParse(slot, out TimeSpan slotStart)) continue;

                if (targetDate == now.Date && now.TimeOfDay.Add(TimeSpan.FromHours(1)) > slotStart)
                    continue;

                TimeSpan slotEnd = slotStart.Add(reservationDuration);
                bool isSlotAvailable = true;

                for (var checkTime = slotStart; checkTime < slotEnd; checkTime += TimeSpan.FromMinutes(30))
                {
                    int occupiedTablesAtCheckTime = existingIntervals
                        .Count(r => r.Start <= checkTime && r.End > checkTime);

                    if (occupiedTablesAtCheckTime >= maxTables)
                    {
                        isSlotAvailable = false;
                        break;
                    }
                }

                if (isSlotAvailable)
                    response.AvailableTimeSlots.Add(slot);
            }

            response.IsFullyBooked = !response.AvailableTimeSlots.Any();

            return response;
        }

        public async Task<IEnumerable<ReservationListDto>> GetAllReservationsAsync(CancellationToken cancellationToken = default)
        {
            var entities = await _reservationRepository.GetAllAsync(cancellationToken);
            return _mapper.Map<IEnumerable<ReservationListDto>>(entities);
        }

        public async Task<ReservationListDto> GetReservationByIdAsync(Guid reservationId, CancellationToken cancellationToken = default)
        {
            var entities = await _reservationRepository.GetWhereAsync(x => x.ReservationId == reservationId, cancellationToken);
            var reservation = _mapper.Map<IEnumerable<ReservationListDto>>(entities).FirstOrDefault();

            return reservation ?? throw new LogicException("NotFound", "Rezervasyon bulunamadı.");
        }

        public async Task<IEnumerable<ReservationListDto>> GetTodaysReservationListAsync(CancellationToken cancellationToken = default)
        {
            var entities = await _reservationRepository.GetWhereAsync(x=>x.ReservationDate == DateTime.Today, cancellationToken);
            return _mapper.Map<IEnumerable<ReservationListDto>>(entities);
        }

        public async Task<PastReservationByUserDto> GetUserReservationByIdAsync(string userId, Guid reservationId, CancellationToken cancellationToken = default)
        {
            if (!Guid.TryParse(userId, out Guid parsedUserId))
                throw new LogicException("InvalidUserId", "Kullanıcı kimliği geçersiz.");

            var entities = await _reservationRepository.GetWhereAsync(x => x.ReservationId == reservationId && x.AppUserId == parsedUserId, cancellationToken);
            var reservation = _mapper.Map<IEnumerable<PastReservationByUserDto>>(entities).FirstOrDefault();

            return reservation ?? throw new LogicException("NotFound", "Rezervasyon bulunamadı.");
        }

        public async Task<IEnumerable<PastReservationByUserDto>> SeeMyPastReservationsAsync(string userId, CancellationToken cancellationToken = default)
        {
            if (!Guid.TryParse(userId, out Guid parsedUserId))
                throw new LogicException("InvalidUserId", "Kullanıcı kimliği geçersiz.");

            var entities = await _reservationRepository.GetWhereAsync(x => x.AppUserId == parsedUserId, cancellationToken);
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
                reservation.NumberOfGuests != dto.NumberOfGuests ||
                reservation.Message != incomingMessage)
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

            var mailBody = emailTemplate
                .Replace("{{Name}}", reservation.Name)
                .Replace("{{Surname}}", reservation.Surname)
                .Replace("{{NewDate}}", reservation.ReservationDate.ToString("dd.MM.yyyy"))
                .Replace("{{NewTime}}", reservation.ReservationTime)
                .Replace("{{NewGuests}}", reservation.NumberOfGuests.ToString());

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

            var mailBody = emailTemplate
                .Replace("{{Name}}", reservation.Name)
                .Replace("{{Surname}}", reservation.Surname)
                .Replace("{{StatusTitle}}", statusTitle)
                .Replace("{{StatusMessage}}", statusMessage)
                .Replace("{{StatusColor}}", statusColor)
                .Replace("{{Date}}", reservation.ReservationDate.ToString("dd.MM.yyyy"))
                .Replace("{{Time}}", reservation.ReservationTime)
                .Replace("{{Guests}}", reservation.NumberOfGuests.ToString());

            var subject = $"Yummy Restoran - Rezervasyon Bilgilendirmesi ({statusTitle})";

            await _emailService.SendEmailAsync(reservation.Email, subject, mailBody);
        }
    }
}
