using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Yummy.Core.DTOs.TestimonialDTOs;
using Yummy.Core.Exceptions;
using Yummy.Core.Services;

namespace Yummy.WebAPI.Controllers.PublicControllers
{
    [Route("api/testimonials")]
    [Authorize]
    [ApiController]
    public class TestimonialsController : ControllerBase
    {
        private readonly ITestimonialService _testimonialService;

        public TestimonialsController(ITestimonialService testimonialService)
        {
            _testimonialService = testimonialService;
        }

        [HttpPost]
        public async Task<IActionResult> AddTestimonial([FromBody] TestimonialCreateDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                throw new LogicException("InvalidId", "Müşteri bilgileri alınamadı.");

            await _testimonialService.AddTestimonialAsync(userId, dto);
            return Ok("Yorumunuz başarıyla eklenmiştir. Admin onayı yapıldığında bilgilendirme yapılacaktır.");
        }

        [HttpGet("user-past-testimonials")]
        public async Task<IActionResult> GetUsersPastTestimonials()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                throw new LogicException("InvalidId", "Müşteri bilgileri alınamadı.");

            var testimonials = await _testimonialService.GetUsersPastTestimonialsAsync(userId);
            return Ok(testimonials);
        }
    }
}
