using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Yummy.Core.Constants;
using Yummy.Core.DTOs.IngredientRequestDTOs;
using Yummy.Core.Exceptions;
using Yummy.Core.Services;

namespace Yummy.WebAPI.Controllers.EmployeeControllers
{
    // şeflerden gelen malzeme talepleri. çalışanlar (Employee) ve admin tedarik eder veya reddeder; şefe e-posta ile bildirilir.
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Employee}")]
    [Route("api/employee/ingredient-requests")]
    [ApiController]
    public class EmployeeIngredientRequestsController : ControllerBase
    {
        private readonly IIngredientRequestService _requestService;

        public EmployeeIngredientRequestsController(IIngredientRequestService requestService)
        {
            _requestService = requestService;
        }

        // bekleyen talepler için ?status=Pending. en yeni talep önce gelir.
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] IngredientRequestQueryDto query)
        {
            return Ok(await _requestService.GetAllAsync(query));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            return Ok(await _requestService.GetByIdAsync(id));
        }

        // her talep satırı için gerçekte gelen miktar girilir (bulunamayan için 0). gelen miktarlar stoğa eklenir.
        [HttpPut("{id}/supply")]
        public async Task<IActionResult> Supply(Guid id, [FromBody] IngredientRequestSupplyDto dto)
        {
            await _requestService.SupplyAsync(GetUserId(), id, dto);
            return Ok(new { message = "Talep tedarik edildi, malzemeler stoğa eklendi ve şef bilgilendirildi." });
        }

        // red gerekçesi zorunludur ve şefe iletilir.
        [HttpPut("{id}/reject")]
        public async Task<IActionResult> Reject(Guid id, [FromBody] IngredientRequestRejectDto dto)
        {
            await _requestService.RejectAsync(GetUserId(), id, dto);
            return Ok(new { message = "Talep reddedildi ve şef bilgilendirildi." });
        }

        private string GetUserId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new LogicException("InvalidId", "Kullanıcı kimliği alınamadı. Lütfen tekrar giriş yapın.");
    }
}
