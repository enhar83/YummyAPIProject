using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Yummy.Core.Constants;
using Yummy.Core.DTOs.IngredientRequestDTOs;
using Yummy.Core.Exceptions;
using Yummy.Core.Services;

namespace Yummy.WebAPI.Controllers.ChefControllers
{
    // şefin malzeme talepleri. şef sadece kendi taleplerini görür ve yönetir.
    [Authorize(Roles = RoleNames.Chef)]
    [Route("api/chef/ingredient-requests")]
    [ApiController]
    public class ChefIngredientRequestsController : ControllerBase
    {
        private readonly IIngredientRequestService _requestService;

        public ChefIngredientRequestsController(IIngredientRequestService requestService)
        {
            _requestService = requestService;
        }

        // malzemeler GET api/chef/ingredients listesinden seçilir. talep çalışanlara "Pending" olarak düşer.
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] IngredientRequestCreateDto dto)
        {
            var requestId = await _requestService.CreateAsync(GetUserId(), dto);
            return StatusCode(201, new { message = "Malzeme talebiniz çalışanlara iletildi.", ingredientRequestId = requestId });
        }

        // kendi talepleri, en yeni önce: ?status=Pending&page=1&pageSize=20 (status isteğe bağlıdır).
        [HttpGet]
        public async Task<IActionResult> GetMyRequests([FromQuery] IngredientRequestQueryDto query)
        {
            return Ok(await _requestService.GetMyRequestsAsync(GetUserId(), query));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetMyRequestById(Guid id)
        {
            return Ok(await _requestService.GetMyRequestByIdAsync(GetUserId(), id));
        }

        // sadece bekleyen talep iptal edilebilir.
        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> Cancel(Guid id)
        {
            await _requestService.CancelAsync(GetUserId(), id);
            return Ok(new { message = "Malzeme talebiniz iptal edildi." });
        }

        private string GetUserId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new LogicException("InvalidId", "Kullanıcı kimliği alınamadı. Lütfen tekrar giriş yapın.");
    }
}
