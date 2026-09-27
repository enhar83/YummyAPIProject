using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Yummy.Core.Constants;
using Yummy.Core.Exceptions;
using Yummy.Core.Services;

namespace Yummy.WebAPI.Controllers.ChefControllers
{
    // şef paneli. sadece Chef rolündeki ve bir şef profiline bağlı kullanıcılar erişebilir.
    [Authorize(Roles = RoleNames.Chef)]
    [Route("api/chef/profile")]
    [ApiController]
    public class ChefProfileController : ControllerBase
    {
        private readonly IChefService _chefService;

        public ChefProfileController(IChefService chefService)
        {
            _chefService = chefService;
        }

        // panelin açılış verisi: giriş yapmış şefin kendi profili.
        [HttpGet]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                throw new LogicException("InvalidId", "Kullanıcı kimliği alınamadı. Lütfen tekrar giriş yapın.");

            var profile = await _chefService.GetMyProfileAsync(userId);
            return Ok(profile);
        }
    }
}
