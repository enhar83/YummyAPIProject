using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Yummy.Core.Services;

namespace Yummy.WebAPI.Controllers.AdminControllers
{
    [Route("api/admin/reservations")]
    [Authorize(Roles = "Admin")]
    [ApiController]
    public class AdminReservationsController : ControllerBase
    {
        private readonly IReservationService _reservationService;

        public AdminReservationsController(IReservationService reservationService)
        {
            _reservationService = reservationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllReservations()
        {
            var reservations = await _reservationService.GetAllReservationsAsync();
            return Ok(reservations);
        }
    }
}
