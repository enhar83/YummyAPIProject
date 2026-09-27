using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Yummy.Core.Constants;
using Yummy.Core.Services;

namespace Yummy.WebAPI.Controllers.ChefControllers
{
    // şef, malzeme talebi ve günün spesyali için stoktaki malzemeleri görür. stok üzerinde değişiklik yapamaz.
    [Authorize(Roles = RoleNames.Chef)]
    [Route("api/chef/ingredients")]
    [ApiController]
    public class ChefIngredientsController : ControllerBase
    {
        private readonly IIngredientService _ingredientService;

        public ChefIngredientsController(IIngredientService ingredientService)
        {
            _ingredientService = ingredientService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _ingredientService.GetAllAsync());
        }
    }
}
