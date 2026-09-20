using System.Threading;
﻿using System;
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

        public async Task AddTestimonialAsync(string userId, TestimonialCreateDto dto, CancellationToken cancellationToken = default)
        {
            if (!Guid.TryParse(userId, out Guid parsedUserId))
                throw new LogicException("InvalidUserId", "Kullanıcı kimliği geçersiz.");

            bool hasCompletedReservation = await _reservationRepository
                .AnyAsync(r => r.AppUserId == parsedUserId && r.ReservationStatus == ReservationStatus.Completed, cancellationToken);

            if (!hasCompletedReservation)
                throw new LogicException("NotAllowed", "Yorum yapabilmek için restoranımızda tamamlanmış en az bir rezervasyonunuzun olması gerekmektedir.");

            var testimonial = _mapper.Map<Testimonial>(dto);
            testimonial.AppUserId = parsedUserId;

            await _testimonialRepository.AddAsync(testimonial, cancellationToken);
            await _uow.SaveAsync(cancellationToken);
        }

        public async Task DeleteTestimonialAsync(Guid testimonialId, CancellationToken cancellationToken = default)
        {
            var testimonial = await _testimonialRepository.GetByIdAsync(testimonialId, cancellationToken);
            if (testimonial == null)
                throw new LogicException("InvalidId", "Silinmek istenen yoruma ait Id bulunamadı.");

            _testimonialRepository.Remove(testimonial);
            await _uow.SaveAsync(cancellationToken);
        }

        public async Task<IEnumerable<AllTestimonialListDto>> GetAllTestimonialsAsync(CancellationToken cancellationToken = default)
        {
            var entities = await _testimonialRepository.GetAllAsync(cancellationToken);
            var sortedEntities = entities.OrderByDescending(x => x.CreatedDate);
            return _mapper.Map<IEnumerable<AllTestimonialListDto>>(sortedEntities);
        }

        public async Task<IEnumerable<UsersPastTestimonialsListDto>> GetUsersPastTestimonialsAsync(string userId, CancellationToken cancellationToken = default)
        {
            if (!Guid.TryParse(userId, out Guid parsedUserId))
                throw new LogicException("InvalidUserId", "Kullanıcı kimliği geçersiz.");

            var entities = await _testimonialRepository.GetWhereAsync(x => x.AppUserId == parsedUserId, cancellationToken);
            var sortedEntities = entities.OrderByDescending(x => x.CreatedDate);
            return _mapper.Map<IEnumerable<UsersPastTestimonialsListDto>>(sortedEntities);
        }

        public async Task ToggleApproveAsync(Guid testimonialId, CancellationToken cancellationToken = default)
        {
            var testimonial = await _testimonialRepository.GetByIdAsync(testimonialId, cancellationToken);
            if (testimonial == null)
                throw new LogicException("NotFound", "Aranılan yorum bulunamadı.");

            testimonial.IsApproved = !testimonial.IsApproved;

            _testimonialRepository.Update(testimonial);
            await _uow.SaveAsync(cancellationToken);
        }
    }
}
