using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Yummy.Core.DTOs.CategoryDTOs;
using Yummy.Core.DTOs.ChefDTOs;
using Yummy.Core.Services;
using Yummy.Core.Constants;

namespace Yummy.WebAPI.Controllers
{
    [Authorize(Roles = RoleNames.Admin)]
    [Route("api/admin/chefs")]
    [ApiController]

    public class AdminChefsController : ControllerBase
    {
        private readonly IChefService _chefService;

        public AdminChefsController(IChefService chefService)
        {
            _chefService = chefService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var chefs = await _chefService.GetAllAsync();
            return Ok(chefs);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var chef = await _chefService.GetByIdAsync(id);
            return Ok(chef);
        }

        [HttpPost]
        public async Task<IActionResult> Add([FromForm] ChefCreateDto dto)
        {
            await _chefService.AddAsync(dto);
            return StatusCode(201, "Şef başarıyla oluşturuldu.");
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromForm] ChefUpdateDto dto)
        {
            await _chefService.UpdateAsync(dto);
            return Ok("Şef başarıyla güncellendi.");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _chefService.DeleteAsync(id);
            return Ok("Şef başarıyla silindi.");
        }

        // şef profilini kullanıcı hesabına bağlar; kullanıcıya Chef rolü verilir ve tekrar giriş yapması gerekir.
        [HttpPut("{id}/link-user")]
        public async Task<IActionResult> LinkUser(Guid id, [FromBody] ChefLinkUserDto dto)
        {
            await _chefService.LinkUserAsync(id, dto);
            return Ok(new { message = "Şef profili kullanıcı hesabına bağlandı. Kullanıcının şef paneline erişmek için tekrar giriş yapması gerekir." });
        }

        // bağlantıyı kaldırır; kullanıcının Chef rolü geri alınır.
        [HttpDelete("{id}/link-user")]
        public async Task<IActionResult> UnlinkUser(Guid id)
        {
            await _chefService.UnlinkUserAsync(id);
            return Ok(new { message = "Şef profili ile kullanıcı hesabı arasındaki bağlantı kaldırıldı." });
        }
    }
}
