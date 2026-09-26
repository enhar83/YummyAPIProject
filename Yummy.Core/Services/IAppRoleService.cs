using System.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yummy.Core.DTOs.AppRoleDTOs;
using Yummy.Core.DTOs.AppUserDTOs;

namespace Yummy.Core.Services
{
    public interface IAppRoleService
    {
        Task CreateRoleAsync(AppRoleCreateDto dto, CancellationToken cancellationToken = default);
        Task<IEnumerable<AppRoleListDto>> GetAllRolesAsync(CancellationToken cancellationToken = default);
        Task UpdateRoleAsync(AppRoleUpdateDto dto, CancellationToken cancellationToken = default);
        Task DeleteRoleAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<AppUserListDto>> GetAllUsersInRoleAsync(Guid roleId, CancellationToken cancellationToken = default);
    }
}
