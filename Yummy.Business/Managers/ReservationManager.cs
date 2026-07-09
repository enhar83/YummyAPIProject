using System;
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

            if (reservation.ReservationStatus == ReservationStatus.Cancelled)
                throw new LogicException("AlreadyCancelled", "Bu rezervasyon zaten daha önce iptal edilmiş.");

            var reservationDateTime = reservation.ReservationDate.Date.AddHours(int.Parse(reservation.ReservationTime.Split(':')[0]));

            if (reservationDateTime < DateTime.Now.AddHours(1))
                throw new LogicException("TooLate", "Rezervasyon saatinize 2 saatten az kaldığı için iptal işlemi yapılamaz.");

            reservation.ReservationStatus = ReservationStatus.Cancelled;

            _reservationRepository.Update(reservation);
            await _uow.SaveAsync();

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

        public async Task<IEnumerable<ReservationListDto>> GetAllReservationsAsync()
        {
            return await _reservationRepository.GetAsQueryable()
                .ProjectTo<ReservationListDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }

        public async Task<PastReservationByUserDto> GetUserReservationByIdAsync(string userId, Guid reservationId)
        {
            if (!Guid.TryParse(userId, out Guid parsedUserId))
                throw new LogicException("InvalidUserId", "Kullanıcı kimliği geçersiz.");

            var reservation = await _reservationRepository.GetAsQueryable()
                .Where(x => x.ReservationId == reservationId && x.AppUserId == parsedUserId)
                .ProjectTo<PastReservationByUserDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();

            return reservation ?? throw new LogicException("NotFound", "Rezervasyon bulunamadı.");
        }

        public async Task<IEnumerable<PastReservationByUserDto>> SeeMyPastReservationsAsync(string userId)
        {
            if (!Guid.TryParse(userId, out Guid parsedUserId))
                throw new LogicException("InvalidUserId", "Kullanıcı kimliği geçersiz.");

            return await _reservationRepository.GetAsQueryable()
                 .Where(x => x.AppUserId == parsedUserId)
                 .OrderByDescending(x => x.ReservationDate)
                 .ProjectTo<PastReservationByUserDto>(_mapper.ConfigurationProvider)
                 .ToListAsync();
        }

        public async Task UpdateReservationStatusAsync(UpdateReservationStatusDto dto)
        {
            var reservation = await _reservationRepository.GetByIdAsync(dto.ReservationId);
            if (reservation == null)
                throw new LogicException("NotFound", "Rezervasyon bulunamadı.");

            reservation.ReservationStatus = dto.ReservationStatus;
            _reservationRepository.Update(reservation);
            await _uow.SaveAsync();
        }
    }
}
