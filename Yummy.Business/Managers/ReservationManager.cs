using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
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

        public async Task AddReservationAsync(string userId, ReservationCreateDto dto)
        {
            var reservation = _mapper.Map<Reservation>(dto);

            if (!Guid.TryParse(userId, out Guid parsedUserId))
                throw new LogicException("InvalidUserId", "Kullanıcı kimliği geçersiz veya doğrulanamadı.");

            reservation.AppUserId = parsedUserId;

            await _reservationRepository.AddAsync(reservation);
            await _uow.SaveAsync();

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

        public async Task CancelReservationAsync(string userId, Guid reservationId)
        {
            var reservation = await _reservationRepository.GetByIdAsync(reservationId);
            if (reservation == null)
                throw new LogicException("NotFound", "Rezervasyon bulunamadı.");

            if (reservation.AppUserId.ToString() != userId)
                throw new LogicException("Forbidden", "Bu işlem için yetkiniz yok.");

            var reservationDateTime = reservation.ReservationDate.Date.AddHours(int.Parse(reservation.ReservationTime.Split(':')[0]));

            if (reservationDateTime < DateTime.Now.AddHours(1))
                throw new LogicException("TooLate", "Rezervasyon saatinize 2 saatten az kaldığı için iptal işlemi yapılamaz.");

            reservation.ReservationStatus = ReservationStatus.Cancelled;

            _reservationRepository.Update(reservation);
            await _uow.SaveAsync();
        }

        public async Task<IEnumerable<PastReservationByUserDto>> SeeMyPastReservationsAsync(string userId)
        {
            if (!Guid.TryParse(userId, out Guid parsedUserId))
                throw new LogicException("InvalidUserId", "Kullanıcı kimliği geçersiz.");
            var reservations = await _reservationRepository.GetWhereAsync(x => x.AppUserId == parsedUserId);

            var reservationDtos = _mapper.Map<IEnumerable<PastReservationByUserDto>>(reservations);
            return reservationDtos.OrderByDescending(x => x.ReservationDate);
        }
    }
}
