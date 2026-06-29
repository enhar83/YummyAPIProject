using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Yummy.Core.DTOs.CategoryDTOs;
using Yummy.Core.DTOs.ChefDTOs;
using Yummy.Core.Services;

namespace Yummy.WebAPI.Controllers
{
    [Authorize]
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
    }
}
