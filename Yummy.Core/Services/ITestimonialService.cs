using System.Threading;
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yummy.Core.DTOs.TestimonialDTOs;

namespace Yummy.Core.Services
{
    public interface ITestimonialService
    {
        Task AddTestimonialAsync(string userId, TestimonialCreateDto dto, CancellationToken cancellationToken = default);
        Task<IEnumerable<UsersPastTestimonialsListDto>> GetUsersPastTestimonialsAsync(string userId, CancellationToken cancellationToken = default);
        Task<IEnumerable<AllTestimonialListDto>> GetAllTestimonialsAsync(CancellationToken cancellationToken = default);
        Task ToggleApproveAsync(Guid testimonialId, CancellationToken cancellationToken = default);
        Task DeleteTestimonialAsync(Guid testimonialId, CancellationToken cancellationToken = default);
    }
}
