using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Yummy.Core.DTOs.AppRoleDTOs;
using Yummy.Core.Services;

namespace Yummy.WebAPI.Controllers.AdminControllers
{
    [Route("api/admin/roles")]
    [Authorize]
    [ApiController]
    public class AdminRolesController : ControllerBase
    {
        private readonly IAppRoleService _roleService;

        public AdminRolesController(IAppRoleService roleService)
        {
            _roleService = roleService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllRoles()
        {
            var roles = await _roleService.GetAllRolesAsync();
            return Ok(roles);
        }

        [HttpPost]
        public async Task<IActionResult> CreateRole([FromBody] AppRoleCreateDto dto)
        {
            await _roleService.CreateRoleAsync(dto);
            return Ok(new { message = "Rol başarıyla oluşturuldu." });
        }

        [HttpPut]
        public async Task<IActionResult> UpdateRole([FromBody] AppRoleUpdateDto dto)
        {
            await _roleService.UpdateRoleAsync(dto);
            return Ok(new { message = "Rol başarıyla güncellendi." });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRole(Guid id)
        {
            await _roleService.DeleteRoleAsync(id);
            return Ok(new { message = "Rol başarıyla silindi." });
        }
    }
}
