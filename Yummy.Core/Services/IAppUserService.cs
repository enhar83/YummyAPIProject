using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yummy.Core.DTOs.AppUserDTOs;

namespace Yummy.Core.Services
{
    public interface IAppUserService
    {
        Task RegisterAsync(AppUserRegisterDto dto);
        Task<RefreshTokenResponseDto> LoginAsync(AppUserLoginDto dto);
        Task VerifyEmailAsync(VerifyEmailDto dto);
        Task ForgotPasswordAsync(ForgotPasswordDto dto);
        Task<IEnumerable<AppUserListDto>> GetAllUsersAsync();
        Task<AppUserListDto> GetUserByIdAsync(Guid id);
        Task ResetPasswordAsync(ResetPasswordDto dto);
        Task ChangePasswordAsync(string userId, ChangePasswordDto dto);
        Task AssignRolesToUserAsync(AppUserAssignRoleDto dto);
        Task RemoveRolesToUserAsync(AppUserAssignRoleDto dto);
        Task<RefreshTokenResponseDto> RefreshTokenAsync(RefreshTokenRequestDto dto);
        Task<GetAppUserProfileDto> GetUserProfileAsync(string userId);
        Task UpdateAppUserAsync(string userId, UpdateAppUserDto dto);
        Task EmailChangeRequestAsync(string userId, ChangeEmailRequestDto dto);
        Task EmailChangeConfirmAsync(string userId, ChangeEmailConfirmDto dto);
    }
}
