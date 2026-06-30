using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Yummy.Core.DTOs.FeatureDTOs;
using Yummy.Core.Services;

namespace Yummy.WebAPI.Controllers.AdminControllers
{
    [Authorize(Roles = "Admin")]
    [Route("api/admin/features")]
    [ApiController]
    public class AdminFeaturesController : ControllerBase
    {
        private readonly IFeatureService _featureService;

        public AdminFeaturesController(IFeatureService featureService)
        {
            _featureService = featureService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var features = await _featureService.GetAllAsync();
            return Ok(features);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var feature = await _featureService.GetByIdAsync(id);
            return Ok(feature);
        }

        [HttpPost]
        public async Task<IActionResult> Add([FromForm] FeatureCreateDto dto)
        {
            await _featureService.AddAsync(dto);
            return StatusCode(201, "Özellik başarıyla oluşturuldu.");
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromForm] FeatureUpdateDto dto)
        {
            await _featureService.UpdateAsync(dto);
            return Ok("Özellik başarıyla güncellendi.");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _featureService.DeleteAsync(id);
            return Ok("Özellik başarıyla silindi.");
        }
    }
}
