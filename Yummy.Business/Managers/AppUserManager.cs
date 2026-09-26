using System.Threading;
﻿using System;
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
using Yummy.Core.DTOs.AppUserDTOs;
using Yummy.Core.Exceptions;
using Yummy.Core.Services;
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
        private readonly IWebHostEnvironment _environment;

        public AppUserManager(UserManager<AppUser> userManager, RoleManager<AppRole> roleManager, IMapper mapper, IEmailService emailService, IJwtService jwtService, IWebHostEnvironment environment)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _mapper = mapper;
            _emailService = emailService;
            _jwtService = jwtService;
            _environment = environment;
        }

        public async Task RegisterAsync(AppUserRegisterDto dto, CancellationToken cancellationToken = default)
        {
            var isEmailExist = await _userManager.FindByEmailAsync(dto.Email);
            if (isEmailExist != null)
                throw new LogicException("Email", "Bu e-posta adresi zaten sistemde kayıtlı. Lütfen giriş yapmayı deneyin.");

            var isUsernameExist = await _userManager.FindByNameAsync(dto.Username);
            if (isUsernameExist != null)
                throw new LogicException("Username", "Bu kullanıcı adı zaten alınmış. Lütfen farklı bir kullanıcı adı seçin.");

            var user = _mapper.Map<AppUser>(dto);

            user.ActivationCode = Guid.NewGuid().ToString().Substring(0, 6).ToUpper(); // kullanıcıya 6 haneli bir activation code oluşturulur.
            user.EmailConfirmed = false; // email onayı yapılmadığı için EmailConfirmed propu default olarak false atanır.

            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join(" | ", result.Errors.Select(e => e.Description));
                throw new LogicException("RegisterError", errors);
            }

            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "EmailActivationTemplate.html"); // email şablonu bulunur.
            if (!File.Exists(templatePath))
            {
                throw new LogicException("TemplateError", "E-posta şablonu bulunamadı.");
            }
            var emailTemplate = await File.ReadAllTextAsync(templatePath); // email şablonu okunur.
            

            var mailBody = emailTemplate
                .Replace("{{Name}}", user.Name)
                .Replace("{{Surname}}", user.Surname)
                .Replace("{{ActivationCode}}", user.ActivationCode);

            var subject = "Yummy Restoran - Hesabınızı Doğrulayın";
            await _emailService.SendEmailAsync(user.Email!, subject, mailBody); // kullanıcıya hesap doğrulama maili iletilir.
        }

        public async Task VerifyEmailAsync(VerifyEmailDto dto, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                throw new LogicException("UserNotFound", "Bu e-posta adresine ait bir kullanıcı bulunamadı.");

            if (user.EmailConfirmed)
                throw new LogicException("AlreadyVerified", "Bu hesap zaten daha önce doğrulanmış. Giriş yapabilirsiniz.");

            if (user.ActivationCode != dto.ActivationCode.Trim().ToUpper())
                throw new LogicException("InvalidCode", "Girdiğiniz aktivasyon kodu hatalı veya süresi dolmuş. Lütfen kontrol edin.");

            user.EmailConfirmed = true; // EmailConfirmed propu true'ya çekilir.
            user.ActivationCode = null; // ActivationCode propu null'a çekilir.
            var result = await _userManager.UpdateAsync(user); // kullanıcı güncellenir.

            if (!result.Succeeded)
            {
                var errors = string.Join(" | ", result.Errors.Select(e => e.Description));
                throw new LogicException("VerifyError", errors);
            }
        }

        public async Task ForgotPasswordAsync(ForgotPasswordDto dto, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                throw new LogicException("UserNotFound", "Bu e-posta adresine ait bir kullanıcı bulunamadı.");

            var token = await _userManager.GeneratePasswordResetTokenAsync(user); // şifresini unutan bir kullanıcının yenilemesi için tek kullanımlık güvenli bir token üretir.

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

        public async Task ResetPasswordAsync(ResetPasswordDto dto, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                throw new LogicException("UserNotFound", "Bu e-posta adresine ait bir kullanıcı bulunamadı.");

            var isSameAsOldPassword = await _userManager.CheckPasswordAsync(user, dto.NewPassword);
            if (isSameAsOldPassword)
                throw new LogicException("SamePasswordError", "Yeni şifreniz, eski şifrenizle aynı olamaz. Lütfen farklı bir şifre belirleyin.");

            RevokeSessions(user); // şifre sıfırlanınca açık oturumlar sonlandırılır. ResetPasswordAsync başarılı olursa kullanıcıyı güncellediği için bu değişiklik de kaydedilir.

            var result = await _userManager.ResetPasswordAsync(user, dto.Token, dto.NewPassword); //  ForgotPasswordAsync içerisinde üretilen token burada kontrol edilir.
            if (!result.Succeeded)
            {
                var errors = string.Join(" | ", result.Errors.Select(e => e.Description));
                throw new LogicException("ResetPasswordError", errors);
            }
        }

        public async Task<RefreshTokenResponseDto> LoginAsync(AppUserLoginDto dto, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                throw new LogicException("InvalidCredentials", "Kullanıcı adı veya şifre yanlış.");

            if (await _userManager.IsLockedOutAsync(user)) // hatalı deneme koruması.
                throw new LogicException("AccountLocked", "Çok fazla hatalı giriş denemesi yapıldı. Lütfen birkaç dakika sonra tekrar deneyin.");

            var result = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!result)
            {
                await _userManager.AccessFailedAsync(user); // hatalı deneme sayacı artırılır, limit aşılırsa hesap kilitlenir.

                if (await _userManager.IsLockedOutAsync(user))
                    throw new LogicException("AccountLocked", "Çok fazla hatalı giriş denemesi yapıldı. Lütfen birkaç dakika sonra tekrar deneyin.");

                throw new LogicException("InvalidCredentials", "Kullanıcı adı veya şifre yanlış.");
            }

            // e-posta doğrulama kontrolü şifre kontrolünden sonra yapılır; böylece şifreyi bilmeyen biri e-postanın sistemde kayıtlı olup olmadığını öğrenemez.
            if (!user.EmailConfirmed)
                throw new LogicException("EmailNotVerified", "Lütfen giriş yapmadan önce e-posta adresinize gönderilen kod ile hesabınızı doğrulayın.");

            await _userManager.ResetAccessFailedCountAsync(user); // başarılı girişte hatalı deneme sayacı sıfırlanır.

            var roles = await _userManager.GetRolesAsync(user); // jwt'ye gömülmek için kullanıcı roller alınır.

            var (accessToken, accessTokenExpiresAt) = _jwtService.CreateToken(user, roles); // jwtManager içerisinde token ve bitiş zamanı (UTC) oluşturulur.
            var refreshToken = GenerateRefreshToken(); // jwt süresi dolduğunda yenilemeyi sağlayacak refresh token üretilir.

            user.RefreshToken = refreshToken; // token kontrolü yapılabilmesi için dbye yazılır.
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            var updateResult = await _userManager.UpdateAsync(user); // RefreshToken propu dbye kaydedildiği için bir update işlemi gerçekleşir. 
            if (!updateResult.Succeeded)
                throw new LogicException("LoginError", "Giriş yapılırken token güncellenemedi.");

            return new RefreshTokenResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiryTime = accessTokenExpiresAt
            };
        }

        public async Task ChangePasswordAsync(string userId, ChangePasswordDto dto, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new LogicException("UserNotFound", "Kullanıcı bulunamadı.");

            var isSameAsOldPassword = await _userManager.CheckPasswordAsync(user, dto.NewPassword);
            if (isSameAsOldPassword)
                throw new LogicException("SamePasswordError", "Yeni şifreniz, eski şifrenizle aynı olamaz. Lütfen farklı bir şifre belirleyin.");

            RevokeSessions(user); // şifre değişince diğer cihazlardaki oturumlar sonlandırılır. ChangePasswordAsync başarılı olursa bu değişiklik de kaydedilir.

            var result = await _userManager.ChangePasswordAsync(user, dto.OldPassword, dto.NewPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(" | ", result.Errors.Select(e => e.Description));
                throw new LogicException("ChangePasswordError", errors);
            }
        }

        public async Task<IEnumerable<AppUserListDto>> GetAllUsersAsync(CancellationToken cancellationToken = default)
        {
            var userDtos = await _userManager.Users
                .ProjectTo<AppUserListDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            var allRoles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync(); // Select ile sadece rol isimleri çekilir RAM yorulmaz. 

            var userRolesMap = userDtos.ToDictionary(u => u.Id, u => new List<string>()); // RAM yorulmasın diye dict kullanılarak tüm kullanıcıların rollerini listeler.

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

        public async Task<AppUserListDto> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
                throw new LogicException("UserNotFound", "Belirtilen ID'ye sahip kullanıcı bulunamadı.");

            var userDto = _mapper.Map<AppUserListDto>(user);

            var roles = await _userManager.GetRolesAsync(user);
            userDto.Roles = roles.ToList(); 

            return userDto;
        }

        public async Task AssignRolesToUserAsync(AppUserAssignRoleDto dto, CancellationToken cancellationToken = default)
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

            RevokeSessions(user); // roller token içerisine gömüldüğü için kullanıcının yeni rollerle tekrar giriş yapması sağlanır.

            var result = await _userManager.AddToRolesAsync(user, rolesToAdd);

            if (!result.Succeeded)
            {
                var errors = string.Join(" | ", result.Errors.Select(e => e.Description));
                throw new LogicException("AssignRoleFailed", errors);
            }
        }

        public async Task RemoveRolesToUserAsync(AppUserAssignRoleDto dto, CancellationToken cancellationToken = default)
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

            RevokeSessions(user); // kaldırılan rol, refresh ile yeniden üretilen token'lara taşınmasın diye oturum sonlandırılır.

            var result = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);

            if (!result.Succeeded)
            {
                var errors = string.Join(" | ", result.Errors.Select(e => e.Description));
                throw new LogicException("RemoveRoleFailed", errors);
            }
        }

        public async Task<RefreshTokenResponseDto> RefreshTokenAsync(RefreshTokenRequestDto dto, CancellationToken cancellationToken = default)
        {
            // gönderilen access token'ın imzası doğrulanır; token bizim tarafımızdan üretilmemişse veya değiştirilmişse null döner.
            var userIdFromToken = await _jwtService.GetUserIdFromExpiredTokenAsync(dto.AccessToken);
            if (userIdFromToken == null)
                throw new LogicException("InvalidToken", "Geçersiz erişim anahtarı.");

            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.RefreshToken == dto.RefreshToken, cancellationToken);

            // refresh token, access token'ın ait olduğu kullanıcıya ait olmalıdır.
            if (user == null || user.Id.ToString() != userIdFromToken)
                throw new LogicException("InvalidToken", "Geçersiz yenileme anahtarı.");

            if (user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                throw new LogicException("TokenExpired", "Oturum süreniz tamamen dolmuş. Lütfen tekrar giriş yapın.");

            var roles = await _userManager.GetRolesAsync(user);
            var (newAccessToken, accessTokenExpiresAt) = _jwtService.CreateToken(user, roles);
            var newRefreshToken = GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
                throw new LogicException("RefreshError", "Yeni token oluşturulurken veritabanı güncellenemedi.");

            return new RefreshTokenResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                AccessTokenExpiryTime = accessTokenExpiresAt
            };
        }

        public async Task LogoutAsync(string userId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new LogicException("UserNotFound", "Kullanıcı bulunamadı.");

            RevokeSessions(user); // refresh token silinir ve security stamp yenilenir; elde kalan access token'lar da anında geçersiz olur.

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                throw new LogicException("LogoutError", "Çıkış yapılırken oturum sonlandırılamadı.");
        }

        public async Task<GetAppUserProfileDto> GetUserProfileAsync(string userId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new LogicException("UserNotFound", "Kullanıcı bulunamadı.");

            var userProfileDto = _mapper.Map<GetAppUserProfileDto>(user);
            return userProfileDto;
        }

        public async Task UpdateAppUserAsync(string userId, UpdateAppUserDto dto, CancellationToken cancellationToken = default)
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

        public async Task EmailChangeRequestAsync(string userId, ChangeEmailRequestDto dto, CancellationToken cancellationToken = default)
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

        public async Task EmailChangeConfirmAsync(string userId, ChangeEmailConfirmDto dto, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new LogicException("UserNotFound", "Kullanıcı bulunamadı.");

            RevokeSessions(user); // e-posta değişince açık oturumlar sonlandırılır. ChangeEmailAsync başarılı olursa bu değişiklik de kaydedilir.

            var result = await _userManager.ChangeEmailAsync(user, dto.NewEmail, dto.Token);

            if (!result.Succeeded)
            {
                var errors = string.Join(" | ", result.Errors.Select(e => e.Description));
                throw new LogicException("ChangeEmailError", $"Doğrulama kodu hatalı veya süresi dolmuş. Detay: {errors}");
            }
        }

        #region Refresh Token İşlemleri
        // kısa ömürlü olan Access Token'ın süresi bittiğinde, kullanıcıdan tekrar şifre istemeden yeni bir Access Token alabilmek için kullanılan uzun ömürlü jetonu üretir.
        private string GenerateRefreshToken() 
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        // kullanıcının tüm oturumları sonlandırılır: refresh token silinir ve security stamp yenilenir.
        // access token içerisindeki stamp, her istekte db'deki stamp ile karşılaştırıldığı için (Program.cs -> OnTokenValidated) eski access token'lar da anında geçersiz olur.
        // değişiklikler, bu metottan sonra çağrılan UserManager işleminin (UpdateAsync, ChangePasswordAsync, AddToRolesAsync vb.) kullanıcıyı güncellemesiyle kaydedilir.
        private static void RevokeSessions(AppUser user)
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            user.SecurityStamp = Convert.ToHexString(RandomNumberGenerator.GetBytes(20));
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
