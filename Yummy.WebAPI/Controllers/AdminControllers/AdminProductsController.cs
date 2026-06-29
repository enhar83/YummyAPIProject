using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Yummy.Core.DTOs.ProductDTOs;
using Yummy.Core.Services;

namespace Yummy.WebAPI.Controllers.AdminControllers
{
    [Authorize]
    [Route("api/admin/products")]
    [ApiController]
    public class AdminProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public AdminProductsController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var products = await _productService.GetAllAsync();
            return Ok(products);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var product = await _productService.GetByIdAsync(id);
            return Ok(product);
        }

        [HttpPost]
        public async Task<IActionResult> Add([FromForm] ProductCreateDto dto)
        {
            await _productService.AddAsync(dto);
            return StatusCode(201, "Ürün başarıyla oluşturuldu.");
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromForm] ProductUpdateDto dto)
        {
            await _productService.UpdateAsync(dto);
            return Ok("Ürün başarıyla güncellendi.");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _productService.DeleteAsync(id);
            return Ok("Ürün başarıyla silindi.");
        }
    }
}
