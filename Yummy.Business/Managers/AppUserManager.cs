using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Yummy.Core.DTOs.AppUserDTOs;
using Yummy.Core.Exceptions;
using Yummy.Core.Services;
using Yummy.Core.Settings;
using Yummy.Entity;

namespace Yummy.Business.Managers
{
    public class AppUserManager : IAppUserService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<AppRole> _roleManager;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailService;
        private readonly IJwtService _jwtService;
        private readonly JwtSettings _jwtSettings;
        private readonly IWebHostEnvironment _environment;

        public AppUserManager(UserManager<AppUser> userManager, RoleManager<AppRole> roleManager, IMapper mapper, IEmailService emailService, IJwtService jwtService, IOptions<JwtSettings> jwtSettings, IWebHostEnvironment environment)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _mapper = mapper;
            _emailService = emailService;
            _jwtService = jwtService;
            _jwtSettings = jwtSettings.Value;
            _environment = environment;
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

        public async Task<RefreshTokenResponseDto> LoginAsync(AppUserLoginDto dto)
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

            var accessToken = _jwtService.CreateToken(user, roles);
            var refreshToken = GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.Now.AddDays(7);

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
                throw new LogicException("LoginError", "Giriş yapılırken token güncellenemedi.");

            return new RefreshTokenResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiryTime = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpiration)
            };
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

        public async Task<IEnumerable<AppUserListDto>> GetAllUsersAsync()
        {
            var userDtos = await _userManager.Users
                .ProjectTo<AppUserListDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            var allRoles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync();

            var userRolesMap = userDtos.ToDictionary(u => u.Id, u => new List<string>());

            foreach (var roleName in allRoles)
            {
                var usersInRole = await _userManager.GetUsersInRoleAsync(roleName);

                foreach (var user in usersInRole)
                {
                    if (userRolesMap.TryGetValue(user.Id, out var rolesList))
                        rolesList.Add(roleName);
                }
            }

            foreach (var dto in userDtos)
            {
                dto.Roles = userRolesMap[dto.Id];
            }

            return userDtos;
        }

        public async Task<AppUserListDto> GetUserByIdAsync(Guid id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
                throw new LogicException("UserNotFound", "Belirtilen ID'ye sahip kullanıcı bulunamadı.");

            var userDto = _mapper.Map<AppUserListDto>(user);

            var roles = await _userManager.GetRolesAsync(user);
            userDto.Roles = roles.ToList(); 

            return userDto;
        }

        public async Task AssignRolesToUserAsync(AppUserAssignRoleDto dto)
        {
            var user = await _userManager.FindByIdAsync(dto.UserId.ToString());
            if (user == null)
                throw new LogicException("UserNotFound", "Kullanıcı sistemde bulunamadı.");

            var existingRoles = await _userManager.GetRolesAsync(user);
            var rolesToAdd = dto.RoleNames
                .Where(newRole => !existingRoles.Contains(newRole))
                .ToList();

            if (!rolesToAdd.Any())
                throw new LogicException("NoNewRoles", "Seçilen roller kullanıcıda zaten mevcut, ekleyecek yeni bir rol bulunamadı.");

            foreach (var roleName in rolesToAdd)
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                    throw new LogicException("RoleNotFound", $"'{roleName}' isminde bir rol sistemde bulunmamaktadır.");
            }

            var result = await _userManager.AddToRolesAsync(user, rolesToAdd);

            if (!result.Succeeded)
            {
                var errors = string.Join(" | ", result.Errors.Select(e => e.Description));
                throw new LogicException("AssignRoleFailed", errors);
            }
        }

        public async Task RemoveRolesToUserAsync(AppUserAssignRoleDto dto)
        {
            var user = await _userManager.FindByIdAsync(dto.UserId.ToString());
            if (user == null)
                throw new LogicException("UserNotFound", "Kullanıcı sistemde bulunamadı.");

            var existingRoles = await _userManager.GetRolesAsync(user);
            var rolesToRemove = dto.RoleNames
                .Where(roleName => existingRoles.Contains(roleName))
                .ToList();

            if (!rolesToRemove.Any())
                throw new LogicException("NoMatchingRoles", "Seçilen roller kullanıcıda mevcut değil, kaldırılacak bir rol bulunamadı.");

            foreach (var roleName in rolesToRemove)
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                    throw new LogicException("RoleNotFound", $"'{roleName}' isminde bir rol sistemde bulunmamaktadır.");
            }

            var result = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);

            if (!result.Succeeded)
            {
                var errors = string.Join(" | ", result.Errors.Select(e => e.Description));
                throw new LogicException("RemoveRoleFailed", errors);
            }
        }

        public async Task<RefreshTokenResponseDto> RefreshTokenAsync(RefreshTokenRequestDto dto)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.RefreshToken == dto.RefreshToken);

            if (user == null)
                throw new LogicException("InvalidToken", "Geçersiz yenileme anahtarı.");

            if (user.RefreshTokenExpiryTime <= DateTime.Now)
                throw new LogicException("TokenExpired", "Oturum süreniz tamamen dolmuş. Lütfen tekrar giriş yapın.");

            var roles = await _userManager.GetRolesAsync(user);
            var newAccessToken = _jwtService.CreateToken(user, roles);
            var newRefreshToken = GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.Now.AddDays(7);

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
                throw new LogicException("RefreshError", "Yeni token oluşturulurken veritabanı güncellenemedi.");

            return new RefreshTokenResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                AccessTokenExpiryTime = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpiration)
            };
        }

        public async Task<GetAppUserProfileDto> GetUserProfileAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new LogicException("UserNotFound", "Kullanıcı bulunamadı.");

            var userProfileDto = _mapper.Map<GetAppUserProfileDto>(user);
            return userProfileDto;
        }

        public async Task UpdateAppUserAsync(string userId, UpdateAppUserDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new LogicException("UserNotFound", "Kullanıcı bulunamadı.");

            if (user.UserName != dto.Username)
            {
                var isUsernameExist = await _userManager.FindByNameAsync(dto.Username);
                if (isUsernameExist != null)
                    throw new LogicException("UsernameTaken", "Bu kullanıcı adı zaten kullanılıyor. Lütfen başka bir tane seçin.");
            }

            var oldImageUrl = user.ImageUrl;
            _mapper.Map(dto, user);

            if (dto.Image != null)
                user.ImageUrl = await SaveFileAsync(dto.Image);
            else
                user.ImageUrl = oldImageUrl;


            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(" | ", result.Errors.Select(e => e.Description));
                throw new LogicException("UpdateProfileError", errors);
            }

            if (dto.Image != null && !string.IsNullOrEmpty(oldImageUrl))
                DeleteFile(oldImageUrl);
        }

        public async Task EmailChangeRequestAsync(string userId, ChangeEmailRequestDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new LogicException("UserNotFound", "Kullanıcı bulunamadı.");

            if (user.Email == dto.NewEmail)
                throw new LogicException("SameEmail", "Yeni mail adresiniz eskisi ile aynı olamaz.");

            var isEmailTaken = await _userManager.FindByEmailAsync(dto.NewEmail);
            if (isEmailTaken != null)
                throw new LogicException("EmailTaken", "Bu mail adresi başka bir kullanıcıya ait.");

            var token = await _userManager.GenerateChangeEmailTokenAsync(user, dto.NewEmail);

            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "EmailChangeTemplate.html");
            if (!File.Exists(templatePath))
                throw new LogicException("TemplateError", "E-posta şablonu bulunamadı.");

            var emailTemplate = await File.ReadAllTextAsync(templatePath);
            var mailBody = emailTemplate
                .Replace("{{Name}}", user.Name)
                .Replace("{{Token}}", token)
                .Replace("{{NewEmail}}", dto.NewEmail);

            var subject = "Yummy Restoran - E-posta Değiştirme Talebi";
            await _emailService.SendEmailAsync(dto.NewEmail, subject, mailBody);
        }

        public async Task EmailChangeConfirmAsync(string userId, ChangeEmailConfirmDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new LogicException("UserNotFound", "Kullanıcı bulunamadı.");

            var result = await _userManager.ChangeEmailAsync(user, dto.NewEmail, dto.Token);

            if (!result.Succeeded)
            {
                var errors = string.Join(" | ", result.Errors.Select(e => e.Description));
                throw new LogicException("ChangeEmailError", $"Doğrulama kodu hatalı veya süresi dolmuş. Detay: {errors}");
            }
        }

        #region Refresh Token İşlemleri
        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
        #endregion

        #region Dosya İşlemleri
        private async Task<string> SaveFileAsync(IFormFile file)
        {
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "user-images");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return $"/images/user-images/{uniqueFileName}";
        }

        private void DeleteFile(string? imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return;
            var filePath = Path.Combine(_environment.WebRootPath, imageUrl.TrimStart('/'));

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        #endregion
    }
}
