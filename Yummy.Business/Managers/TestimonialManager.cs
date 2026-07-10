using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Yummy.Core.DTOs.ReservationDTOs;
using Yummy.Core.DTOs.TestimonialDTOs;
using Yummy.Core.Exceptions;
using Yummy.Core.IRepositories;
using Yummy.Core.IUnitOfWork;
using Yummy.Core.Services;
using Yummy.Entity;
using Yummy.Entity.Enums;

namespace Yummy.Business.Managers
{
    public class TestimonialManager : ITestimonialService
    {
        private readonly IGenericRepository<Testimonial> _testimonialRepository;
        private readonly IGenericRepository<Reservation> _reservationRepository;
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public TestimonialManager(IGenericRepository<Testimonial> testimonialRepository, IGenericRepository<Reservation> reservationRepository, IUnitOfWork uow, IMapper mapper)
        {
            _testimonialRepository = testimonialRepository;
            _reservationRepository = reservationRepository;
            _uow = uow;
            _mapper = mapper;
        }

        public async Task AddTestimonialAsync(string userId, TestimonialCreateDto dto)
        {
            if (!Guid.TryParse(userId, out Guid parsedUserId))
                throw new LogicException("InvalidUserId", "Kullanıcı kimliği geçersiz.");

            bool hasCompletedReservation = await _reservationRepository.GetAsQueryable()
                .AnyAsync(r => r.AppUserId == parsedUserId && r.ReservationStatus == ReservationStatus.Completed);

            if (!hasCompletedReservation)
                throw new LogicException("NotAllowed", "Yorum yapabilmek için restoranımızda tamamlanmış en az bir rezervasyonunuzun olması gerekmektedir.");

            var testimonial = _mapper.Map<Testimonial>(dto);
            testimonial.AppUserId = parsedUserId;

            await _testimonialRepository.AddAsync(testimonial);
            await _uow.SaveAsync();
        }

        public async Task DeleteTestimonialAsync(Guid testimonialId)
        {
            var testimonial = await _testimonialRepository.GetByIdAsync(testimonialId);
            if (testimonial == null)
                throw new LogicException("InvalidId", "Silinmek istenen yoruma ait Id bulunamadı.");

            _testimonialRepository.Remove(testimonial);
            await _uow.SaveAsync();
        }

        public async Task<IEnumerable<AllTestimonialListDto>> GetAllTestimonialsAsync()
        {
            return await _testimonialRepository.GetAsQueryable()
                 .OrderByDescending(x => x.CreatedDate)
                 .ProjectTo<AllTestimonialListDto>(_mapper.ConfigurationProvider)
                 .ToListAsync();
        }

        public async Task<IEnumerable<UsersPastTestimonialsListDto>> GetUsersPastTestimonialsAsync(string userId)
        {
            if (!Guid.TryParse(userId, out Guid parsedUserId))
                throw new LogicException("InvalidUserId", "Kullanıcı kimliği geçersiz.");

            return await _testimonialRepository.GetAsQueryable()
                 .Where(x => x.AppUserId == parsedUserId)
                 .OrderByDescending(x => x.CreatedDate)
                 .ProjectTo<UsersPastTestimonialsListDto>(_mapper.ConfigurationProvider)
                 .ToListAsync();
        }

        public async Task ToggleApproveAsync(Guid testimonialId)
        {
            var testimonial = await _testimonialRepository.GetByIdAsync(testimonialId);
            if (testimonial == null)
                throw new LogicException("NotFound", "Aranılan yorum bulunamadı.");

            testimonial.IsApproved = !testimonial.IsApproved;

            _testimonialRepository.Update(testimonial);
            await _uow.SaveAsync();
        }
    }
}
