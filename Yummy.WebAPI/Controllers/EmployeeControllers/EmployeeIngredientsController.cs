using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Yummy.Core.Constants;
using Yummy.Core.DTOs.CommonDTOs;
using Yummy.Core.DTOs.IngredientDTOs;
using Yummy.Core.Exceptions;
using Yummy.Core.Services;

namespace Yummy.WebAPI.Controllers.EmployeeControllers
{
    // stok kartları ve stok hareketleri. çalışanlar (Employee) ve admin erişebilir.
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Employee}")]
    [Route("api/employee/ingredients")]
    [ApiController]
    public class EmployeeIngredientsController : ControllerBase
    {
        private readonly IIngredientService _ingredientService;

        public EmployeeIngredientsController(IIngredientService ingredientService)
        {
            _ingredientService = ingredientService;
        }

        // ada göre sıralı tüm stok kartları ve mevcut stok miktarları.
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _ingredientService.GetAllAsync());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            return Ok(await _ingredientService.GetByIdAsync(id));
        }

        // yeni kart stok 0 ile oluşur; ilk miktar stok girişi ile eklenir.
        [HttpPost]
        public async Task<IActionResult> Add([FromBody] IngredientCreateDto dto)
        {
            await _ingredientService.AddAsync(dto);
            return StatusCode(201, new { message = "Malzeme başarıyla oluşturuldu." });
        }

        // sadece ad ve birim güncellenir; stok hareketi olan malzemenin birimi değiştirilemez.
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] IngredientUpdateDto dto)
        {
            await _ingredientService.UpdateAsync(dto);
            return Ok(new { message = "Malzeme başarıyla güncellendi." });
        }

        // stokta miktar varken silinemez.
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _ingredientService.DeleteAsync(id);
            return Ok(new { message = "Malzeme başarıyla silindi." });
        }

        // type: 1 = stok girişi, 2 = fire, 3 = sayım düzeltmesi (quantity sayılan gerçek miktardır).
        [HttpPost("{id}/stock-adjustments")]
        public async Task<IActionResult> AdjustStock(Guid id, [FromBody] StockAdjustmentDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                throw new LogicException("InvalidId", "Kullanıcı kimliği alınamadı. Lütfen tekrar giriş yapın.");

            await _ingredientService.AdjustStockAsync(userId, id, dto);
            return Ok(new { message = "Stok başarıyla güncellendi." });
        }

        // sayfalı hareket geçmişi: ?page=1&pageSize=20. en yeni hareket önce gelir.
        [HttpGet("{id}/stock-movements")]
        public async Task<IActionResult> GetStockMovements(Guid id, [FromQuery] PaginationQueryDto query)
        {
            return Ok(await _ingredientService.GetStockMovementsAsync(id, query));
        }
    }
}
