using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Yummy.Core.DTOs.CommonDTOs;
using Yummy.Core.DTOs.ReservationDTOs;
using Yummy.Core.Exceptions;
using Yummy.Core.Services;
using Yummy.Core.Constants;

namespace Yummy.WebAPI.Controllers.AdminControllers
{
    [Route("api/admin/reservations")]
    [Authorize(Roles = RoleNames.Admin)]
    [ApiController]
    public class AdminReservationsController : ControllerBase
    {
        private readonly IReservationService _reservationService;

        public AdminReservationsController(IReservationService reservationService)
        {
            _reservationService = reservationService;
        }

        // sayfalı liste: ?page=1&pageSize=20 (pageSize en fazla 100). en yeni tarihli rezervasyonlar önce gelir.
        [HttpGet]
        public async Task<IActionResult> GetAllReservations([FromQuery] PaginationQueryDto query)
        {
            var reservations = await _reservationService.GetAllReservationsAsync(query);
            return Ok(reservations);
        }

        // durum kuralları: aynı duruma geçilemez, tamamlanmış rezervasyon değiştirilemez, Completed elle atanamaz (validator).
        // iptal edilmiş rezervasyon tekrar aktif edilirken masanın hâlâ aktif ve o saatte boş olduğu kontrol edilir. her değişiklikte müşteriye e-posta gider.
        [HttpPut("update-status")]
        public async Task<IActionResult> UpdateReservationStatus([FromBody] UpdateReservationDto dto)
        {
            await _reservationService.UpdateReservationStatusAsync(dto);
            return Ok("Rezervasyon durumu başarıyla güncellendi.");
        }

        [HttpGet("get-reservation/{id}")]
        public async Task<IActionResult> GetReservationById(Guid id)
        {
            var reservation = await _reservationService.GetReservationByIdAsync(id);
            return Ok(reservation);
        }

        // restoranın saat dilimine göre bugünün tüm rezervasyonları, saate göre sıralı.
        [HttpGet("todays-reservations")]
        public async Task<IActionResult> GetTodaysReservations()
        {
            var reservations = await _reservationService.GetTodaysReservationListAsync();
            return Ok(reservations);
        }
    }
}
