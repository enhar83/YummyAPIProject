using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Yummy.Core.DTOs.AppUserDTOs;
using Yummy.Core.Exceptions;
using Yummy.Core.Services;
using Yummy.Entity;

namespace Yummy.Business.Managers
{
    public class AppUserManager : IAppUserService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailService;
        private readonly IJwtService _jwtService;

        public AppUserManager(UserManager<AppUser> userManager, IMapper mapper, IEmailService emailService, IJwtService jwtService)
        {
            _userManager = userManager;
            _mapper = mapper;
            _emailService = emailService;
            _jwtService = jwtService;
        }

        public async Task RegisterAsync(AppUserRegisterDto dto)
        {
            var isEmailExist = await _userManager.FindByEmailAsync(dto.Email);
            if (isEmailExist != null)
                throw new LogicException("Email", "Bu e-posta adresi zaten sistemde kayıtlı. Lütfen giriş yapmayı deneyin.");

            var isUsernameExist = await _userManager.FindByNameAsync(dto.Username);
            if (isUsernameExist != null)
                throw new LogicException("Username", "Bu kullanıcı adı zaten alınmış. Lütfen farklı bir kullanıcı adı seçin.");

            var user = _mapper.Map<AppUser>(dto);

            user.ActivationCode = Guid.NewGuid().ToString().Substring(0, 6).ToUpper();
            user.EmailConfirmed = false;

            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join(" | ", result.Errors.Select(e => e.Description));
                throw new LogicException("RegisterError", errors);
            }

            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "EmailActivationTemplate.html");
            if (!File.Exists(templatePath))
            {
                throw new LogicException("TemplateError", "E-posta şablonu bulunamadı.");
            }
            var emailTemplate = await File.ReadAllTextAsync(templatePath);

            var mailBody = emailTemplate
                .Replace("{{Name}}", user.Name)
                .Replace("{{Surname}}", user.Surname)
                .Replace("{{ActivationCode}}", user.ActivationCode);

            var subject = "Yummy Restoran - Hesabınızı Doğrulayın";
            await _emailService.SendEmailAsync(user.Email!, subject, mailBody);
        }

        public async Task VerifyEmailAsync(VerifyEmailDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                throw new LogicException("UserNotFound", "Bu e-posta adresine ait bir kullanıcı bulunamadı.");

            if (user.EmailConfirmed)
                throw new LogicException("AlreadyVerified", "Bu hesap zaten daha önce doğrulanmış. Giriş yapabilirsiniz.");

            if (user.ActivationCode != dto.ActivationCode.Trim().ToUpper())
                throw new LogicException("InvalidCode", "Girdiğiniz aktivasyon kodu hatalı veya süresi dolmuş. Lütfen kontrol edin.");

            user.EmailConfirmed = true;
            user.ActivationCode = null;
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var errors = string.Join(" | ", result.Errors.Select(e => e.Description));
                throw new LogicException("VerifyError", errors);
            }
        }

        public async Task ForgotPasswordAsync(ForgotPasswordDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                throw new LogicException("UserNotFound", "Bu e-posta adresine ait bir kullanıcı bulunamadı.");

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "EmailResetPasswordTemplate.html");
            if (!File.Exists(templatePath))
                throw new LogicException("TemplateError", "Şifre sıfırlama şablonu bulunamadı.");

            var emailTemplate = await File.ReadAllTextAsync(templatePath);

            var mailBody = emailTemplate
                .Replace("{{Name}}", user.Name)
                .Replace("{{Surname}}", user.Surname) 
                .Replace("{{Token}}", token); 

            var subject = "Yummy Restoran - Şifre Sıfırlama Talebi";

            await _emailService.SendEmailAsync(user.Email!, subject, mailBody);
        }

        public async Task ResetPasswordAsync(ResetPasswordDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                throw new LogicException("UserNotFound", "Bu e-posta adresine ait bir kullanıcı bulunamadı.");

            var isSameAsOldPassword = await _userManager.CheckPasswordAsync(user, dto.NewPassword);
            if (isSameAsOldPassword)
                throw new LogicException("SamePasswordError", "Yeni şifreniz, eski şifrenizle aynı olamaz. Lütfen farklı bir şifre belirleyin.");

            var result = await _userManager.ResetPasswordAsync(user, dto.Token, dto.NewPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(" | ", result.Errors.Select(e => e.Description));
                throw new LogicException("ResetPasswordError", errors);
            }
        }

        public async Task<string> LoginAsync(AppUserLoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                throw new LogicException("UserNotFound", "Kullanıcı adı veya şifre yanlış.");

            if (!user.EmailConfirmed)
                throw new LogicException("EmailNotVerified", "Lütfen giriş yapmadan önce e-posta adresinize gönderilen kod ile hesabınızı doğrulayın.");

            var result = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!result)
                throw new LogicException("InvalidCredentials", "Kullanıcı adı veya şifre yanlış.");

            var roles = await _userManager.GetRolesAsync(user);

            var token = _jwtService.CreateToken(user, roles);
            return token;
        }

        public async Task ChangePasswordAsync(string userId, ChangePasswordDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new LogicException("UserNotFound", "Kullanıcı bulunamadı.");

            var isSameAsOldPassword = await _userManager.CheckPasswordAsync(user, dto.NewPassword);
            if (isSameAsOldPassword)
                throw new LogicException("SamePasswordError", "Yeni şifreniz, eski şifrenizle aynı olamaz. Lütfen farklı bir şifre belirleyin.");

            var result = await _userManager.ChangePasswordAsync(user, dto.OldPassword, dto.NewPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(" | ", result.Errors.Select(e => e.Description));
                throw new LogicException("ChangePasswordError", errors);
            }
        }
    }
}
