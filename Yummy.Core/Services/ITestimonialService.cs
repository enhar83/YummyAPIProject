using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yummy.Core.DTOs.TestimonialDTOs;

namespace Yummy.Core.Services
{
    public interface ITestimonialService
    {
        Task AddTestimonialAsync(string userId, TestimonialCreateDto dto);
        Task<IEnumerable<UsersPastTestimonialsListDto>> GetUsersPastTestimonialsAsync(string userId);
        Task<IEnumerable<AllTestimonialListDto>> GetAllTestimonialsAsync();
        Task ToggleApproveAsync(Guid testimonialId);
    }
}
