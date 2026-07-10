using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Yummy.Core.Services;

namespace Yummy.WebAPI.Controllers.AdminControllers
{
    [Route("api/admin/testimonials")]
    [Authorize(Roles = "Admin")]
    [ApiController]
    public class AdminTestimonialsController : ControllerBase
    {
        private readonly ITestimonialService _testimonialService;
        public AdminTestimonialsController(ITestimonialService testimonialService)
        {
            _testimonialService = testimonialService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllTestimonials()
        {
            var testimonials = await _testimonialService.GetAllTestimonialsAsync();
            return Ok(testimonials);
        }
    }
}
