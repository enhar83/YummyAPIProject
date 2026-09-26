using System.Threading;
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
        Task RegisterAsync(AppUserRegisterDto dto, CancellationToken cancellationToken = default);
        Task<RefreshTokenResponseDto> LoginAsync(AppUserLoginDto dto, CancellationToken cancellationToken = default);
        Task VerifyEmailAsync(VerifyEmailDto dto, CancellationToken cancellationToken = default);
        Task ResendActivationCodeAsync(ResendActivationCodeDto dto, CancellationToken cancellationToken = default);
        Task ForgotPasswordAsync(ForgotPasswordDto dto, CancellationToken cancellationToken = default);
        Task<IEnumerable<AppUserListDto>> GetAllUsersAsync(CancellationToken cancellationToken = default);
        Task<AppUserListDto> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task ResetPasswordAsync(ResetPasswordDto dto, CancellationToken cancellationToken = default);
        Task ChangePasswordAsync(string userId, ChangePasswordDto dto, CancellationToken cancellationToken = default);
        Task AssignRolesToUserAsync(AppUserAssignRoleDto dto, CancellationToken cancellationToken = default);
        Task RemoveRolesToUserAsync(AppUserAssignRoleDto dto, CancellationToken cancellationToken = default);
        Task<RefreshTokenResponseDto> RefreshTokenAsync(RefreshTokenRequestDto dto, CancellationToken cancellationToken = default);
        Task LogoutAsync(string userId, CancellationToken cancellationToken = default);
        Task<GetAppUserProfileDto> GetUserProfileAsync(string userId, CancellationToken cancellationToken = default);
        Task UpdateAppUserAsync(string userId, UpdateAppUserDto dto, CancellationToken cancellationToken = default);
        Task EmailChangeRequestAsync(string userId, ChangeEmailRequestDto dto, CancellationToken cancellationToken = default);
        Task EmailChangeConfirmAsync(string userId, ChangeEmailConfirmDto dto, CancellationToken cancellationToken = default);
    }
}
