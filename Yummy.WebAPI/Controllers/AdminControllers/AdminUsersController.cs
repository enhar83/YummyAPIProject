using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Yummy.Core.DTOs.AppUserDTOs;
using Yummy.Core.Services;

namespace Yummy.WebAPI.Controllers.AdminControllers
{
    [Route("api/admin/users")]
    [ApiController]
    public class AdminUsersController : ControllerBase
    {
        private readonly IAppUserService _appUserService;

        public AdminUsersController(IAppUserService appUserService)
        {
            _appUserService = appUserService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _appUserService.GetAllUsersAsync();
            return Ok(users);
        }

        [HttpPost("assign-roles")]
        public async Task<IActionResult> AssignRoles([FromBody] AppUserAssignRoleDto dto)
        {
            await _appUserService.AssignRolesToUserAsync(dto);
            return Ok(new { message = "Roller kullanıcıya başarıyla atandı." });
        }
    }
}
