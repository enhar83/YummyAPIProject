using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Yummy.Core.DTOs.AppUserDTOs;
using Yummy.Core.Exceptions;
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

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] AppUserLoginDto dto)
        {
            var token = await _appUserService.LoginAsync(dto);
            return Ok(token);
        }

        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailDto dto)
        {
            await _appUserService.VerifyEmailAsync(dto);
            return Ok(new { message = "E-posta adresiniz başarıyla doğrulandı. Hesabınız aktif hale getirilmiştir, artık giriş yapabilirsiniz." });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            await _appUserService.ForgotPasswordAsync(dto);
            return Ok(new { message = "Şifre sıfırlama talimatları e-posta adresinize gönderildi. Lütfen e-postanızı kontrol edin." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            await _appUserService.ResetPasswordAsync(dto);
            return Ok(new { message = "Şifreniz başarıyla sıfırlandı. Artık yeni şifrenizle giriş yapabilirsiniz." });
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                throw new LogicException("InvalidId", "Kullanıcı kimliği alınamadı. Lütfen tekrar giriş yapın.");

            await _appUserService.ChangePasswordAsync(userId, dto);
            return Ok(new { message = "Şifreniz başarıyla değiştirildi." });
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto dto)
        {
            var result = await _appUserService.RefreshTokenAsync(dto);
            return Ok(result);
        }

        [Authorize]
        [HttpPut("update-profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateAppUserDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                throw new LogicException("InvalidId", "Kullanıcı kimliği alınamadı. Lütfen tekrar giriş yapın.");

            await _appUserService.UpdateAppUserAsync(userId, dto);
            return Ok(new { message = "Profiliniz başarıyla güncellendi." });
        }
    }
}
