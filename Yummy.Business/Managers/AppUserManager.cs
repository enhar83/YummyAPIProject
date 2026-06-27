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

        public AppUserManager(UserManager<AppUser> userManager, IMapper mapper, IEmailService emailService)
        {
            _userManager = userManager;
            _mapper = mapper;
            _emailService = emailService;
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
    }
}
