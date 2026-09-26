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
    [Route("api/profile")]
    [Authorize]
    [ApiController]
    public class ProfilesController : ControllerBase
    {
        private readonly IAppUserService _appUserService;
        public ProfilesController(IAppUserService appUserService)
        {
            _appUserService = appUserService;
        }

        [HttpPost("change-password")]
        [EnableRateLimiting(RateLimitPolicies.Auth)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                throw new LogicException("InvalidId", "Kullanıcı kimliği alınamadı. Lütfen tekrar giriş yapın.");

            await _appUserService.ChangePasswordAsync(userId, dto);
            return Ok(new { message = "Şifreniz başarıyla değiştirildi." });
        }

        [HttpGet("view-profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                throw new LogicException("InvalidId", "Kullanıcı kimliği alınamadı. Lütfen tekrar giriş yapın.");

            var user = await _appUserService.GetUserProfileAsync(userId);
            return Ok(user);
        }

        [HttpPut("update-profile")]
        public async Task<IActionResult> UpdateProfile([FromForm] UpdateAppUserDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                throw new LogicException("InvalidId", "Kullanıcı kimliği alınamadı. Lütfen tekrar giriş yapın.");

            await _appUserService.UpdateAppUserAsync(userId, dto);
            return Ok(new { message = "Profiliniz başarıyla güncellendi." });
        }

        [HttpPost("request-email-change")]
        [EnableRateLimiting(RateLimitPolicies.EmailSending)]
        public async Task<IActionResult> RequestEmailChange(ChangeEmailRequestDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                throw new LogicException("InvalidId", "Kullanıcı kimliği alınamadı. Lütfen tekrar giriş yapın.");

            await _appUserService.EmailChangeRequestAsync(userId, dto);

            return Ok(new { message = "Yeni e-posta adresinize bir doğrulama kodu gönderildi. Lütfen gelen kutunuzu kontrol edin." });
        }

        [HttpPost("confirm-email-change")]
        public async Task<IActionResult> ConfirmEmailChange(ChangeEmailConfirmDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                throw new LogicException("InvalidId", "Kullanıcı kimliği alınamadı. Lütfen tekrar giriş yapın.");

            await _appUserService.EmailChangeConfirmAsync(userId, dto);

            return Ok(new { message = "E-posta adresiniz başarıyla güncellendi." });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                throw new LogicException("InvalidId", "Kullanıcı kimliği alınamadı. Lütfen tekrar giriş yapın.");

            await _appUserService.LogoutAsync(userId);
            return Ok(new { message = "Başarıyla çıkış yapıldı." });
        }
    }
}
