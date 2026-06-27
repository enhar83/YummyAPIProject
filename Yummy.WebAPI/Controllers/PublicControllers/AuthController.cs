using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Yummy.Core.DTOs.AppUserDTOs;
using Yummy.Core.Services;

namespace Yummy.WebAPI.Controllers.PublicControllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAppUserService _appUserService;

        public AuthController(IAppUserService appUserService)
        {
            _appUserService = appUserService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] AppUserRegisterDto dto)
        {
            await _appUserService.RegisterAsync(dto);
            return Ok(new { message = "Kayıt işlemi başarılı. Lütfen e-posta adresinize gönderilen 6 haneli doğrulama kodunu kontrol edin." });
        }

        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailDto dto)
        {
            await _appUserService.VerifyEmailAsync(dto);
            return Ok(new { message = "E-posta adresiniz başarıyla doğrulandı. Hesabınız aktif hale getirilmiştir, artık giriş yapabilirsiniz." });
        }
    }
}
