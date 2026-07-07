using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Yummy.Core.DTOs.ReservationDTOs;
using Yummy.Core.Exceptions;
using Yummy.Core.Services;

namespace Yummy.WebAPI.Controllers.PublicControllers
{
    [Route("api/reservations")]
    [Authorize]
    [ApiController]
    public class ReservationsController : ControllerBase
    {
        private readonly IReservationService _reservationService;

        public ReservationsController(IReservationService reservationService)
        {
            _reservationService = reservationService;
        }

        [HttpPost("make-reservation")]
        public async Task<IActionResult> CreateReservation([FromBody] ReservationCreateDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                throw new LogicException("InvalidId", "Kullanıcı kimliği alınamadı. Lütfen tekrar giriş yapın.");

            await _reservationService.AddReservationAsync(userId, dto);

            return Ok(new
            {
                message = "Rezervasyon talebiniz başarıyla alındı. Detaylar e-posta adresinize gönderilmiştir."
            });
        }
    }
}
