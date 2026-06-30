using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yummy.Core.DTOs.AppRoleDTOs;

namespace Yummy.Core.Services
{
    public interface IAppRoleService
    {
        Task CreateRoleAsync(AppRoleCreateDto dto);
        Task<IEnumerable<AppRoleListDto>> GetAllRolesAsync();
        Task UpdateRoleAsync(AppRoleUpdateDto dto);
    }
}
