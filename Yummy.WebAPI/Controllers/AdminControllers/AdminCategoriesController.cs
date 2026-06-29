using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Yummy.Core.DTOs.CategoryDTOs;
using Yummy.Core.Services;

namespace Yummy.WebAPI.Controllers
{
    [Authorize]
    [Route("api/admin/categories")]
    [ApiController]

    public class AdminCategoriesController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public AdminCategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var categories = await _categoryService.GetAllAsync();
            return Ok(categories);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var category = await _categoryService.GetByIdAsync(id);
            return Ok(category);
        }

        [HttpPost]
        public async Task<IActionResult> Add(CategoryCreateDto dto)
        {
            await _categoryService.AddAsync(dto);
            return StatusCode(201, "Kategori başarıyla oluşturuldu.");
        }

        [HttpPut]
        public async Task<IActionResult> Update(CategoryUpdateDto dto)
        {
            await _categoryService.UpdateAsync(dto);
            return Ok("Kategori başarıyla güncellendi.");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _categoryService.DeleteAsync(id);
            return Ok("Kategori başarıyla silindi.");
        }
    }
}
