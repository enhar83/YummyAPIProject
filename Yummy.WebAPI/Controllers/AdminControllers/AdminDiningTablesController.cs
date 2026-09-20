using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Yummy.Core.DTOs.DiningTableDTOs;
using Yummy.Core.Services;

namespace Yummy.WebAPI.Controllers.AdminControllers
{
    [Authorize(Roles = "Admin")]
    [Route("api/admin/dining-tables")]
    [ApiController]
    public class AdminDiningTablesController : ControllerBase
    {
        private readonly IDiningTableService _tableService;

        public AdminDiningTablesController(IDiningTableService tableService)
        {
            _tableService = tableService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var tables = await _tableService.GetAllAsync();
            return Ok(tables);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var table = await _tableService.GetByIdAsync(id);
            return Ok(table);
        }

        [HttpPost]
        public async Task<IActionResult> Add(DiningTableCreateDto dto)
        {
            await _tableService.AddAsync(dto);
            return StatusCode(201, "Masa başarıyla eklendi.");
        }

        [HttpPut]
        public async Task<IActionResult> Update(DiningTableUpdateDto dto)
        {
            await _tableService.UpdateAsync(dto);
            return Ok("Masa başarıyla güncellendi.");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _tableService.DeleteAsync(id);
            return Ok("Masa başarıyla silindi.");
        }
    }
}
