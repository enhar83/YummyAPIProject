using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Yummy.Core.DTOs.AppUserDTOs;
using Yummy.Core.Exceptions;
using Yummy.Core.Services;
using Yummy.WebAPI.RateLimiting;

namespace Yummy.WebAPI.Controllers.PublicControllers
{
    [Route("api/auth")]
    [EnableRateLimiting(RateLimitPolicies.Auth)] // tüm auth endpoint'leri için IP bazlı istek sınırı. E-posta gönderenler aşağıda daha sıkı policy ile ezilir.
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAppUserService _appUserService;

        public AuthController(IAppUserService appUserService)
        {
            _appUserService = appUserService;
        }

        [HttpPost("register")]
        [EnableRateLimiting(RateLimitPolicies.EmailSending)]
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

        [HttpPost("resend-activation-code")]
        [EnableRateLimiting(RateLimitPolicies.EmailSending)]
        public async Task<IActionResult> ResendActivationCode([FromBody] ResendActivationCodeDto dto)
        {
            await _appUserService.ResendActivationCodeAsync(dto);
            return Ok(new { message = "Yeni doğrulama kodu e-posta adresinize gönderildi." });
        }

        [HttpPost("forgot-password")]
        [EnableRateLimiting(RateLimitPolicies.EmailSending)]
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

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto dto)
        {
            var result = await _appUserService.RefreshTokenAsync(dto);
            return Ok(result);
        }
    }
}
