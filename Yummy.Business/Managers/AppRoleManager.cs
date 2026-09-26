using System.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Yummy.Core.Constants;
using Yummy.Core.DTOs.AppRoleDTOs;
using Yummy.Core.DTOs.AppUserDTOs;
using Yummy.Core.Exceptions;
using Yummy.Core.Services;
using Yummy.Entity;

namespace Yummy.Business.Managers
{
    public class AppRoleManager : IAppRoleService
    {
        private readonly RoleManager<AppRole> _roleManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly IMapper _mapper;

        public AppRoleManager(RoleManager<AppRole> roleManager,  IMapper mapper, UserManager<AppUser> userManager)
        {
            _roleManager = roleManager;
            _mapper = mapper;
            _userManager = userManager;
        }

        public async Task CreateRoleAsync(AppRoleCreateDto dto, CancellationToken cancellationToken = default)
        {
            var role = _mapper.Map<AppRole>(dto);
            bool isRoleExists = await _roleManager.RoleExistsAsync(role.Name!);
            if (isRoleExists)
            {
                throw new LogicException("RoleExist", "Rol zaten sistem içerisinde kullanılıyor.");
            }

            var result = await _roleManager.CreateAsync(role);
            if (!result.Succeeded)
            {
                var errors = string.Join(" | ", result.Errors.Select(e => e.Description));
                throw new LogicException("RoleCreateFailed", errors);
            }   
        }

        public async Task DeleteRoleAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null)
                throw new LogicException("RoleNotFound", "Silinmek istenen rol sistemde bulunamadı.");

            if (IsSystemRole(role))
                throw new LogicException("ProtectedRole", $"{role.Name} rolü sistem rolüdür ve silinemez.");

            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!); // rol silindikten sonra bu kullanıcılar bulunamayacağı için önceden alınır.

            var result = await _roleManager.DeleteAsync(role);
            if (!result.Succeeded)
            {
                var errors = string.Join(" | ", result.Errors.Select(e => e.Description));
                throw new LogicException("RoleDeleteFailed", errors);
            }

            await RevokeSessionsAsync(usersInRole); // silinen rol token'larda kalmasın diye bu kullanıcıların oturumları sonlandırılır.
        }

        public async Task<IEnumerable<AppRoleListDto>> GetAllRolesAsync(CancellationToken cancellationToken = default)
        {
            var roles = await _roleManager.Roles
                .ProjectTo<AppRoleListDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            return roles;
        }

        public async Task<IEnumerable<AppUserListDto>> GetAllUsersInRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
        {
            var role = await _roleManager.FindByIdAsync(roleId.ToString()); //id'ye göre istenilen rol bulunur.
            if (role == null)
                throw new LogicException("RoleNotFound", "Böyle bir rol bulunamadı.");

            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!); //bu role sahip olan kullanıcılar alınır.
            var userDtos = _mapper.Map<List<AppUserListDto>>(usersInRole); // bu kullanıcılar AppUserListDto dto'suna maplenir.
            for (int i = 0; i < usersInRole.Count; i++)
            {
                var roles = await _userManager.GetRolesAsync(usersInRole[i]); // bu kullanıcıların rolleri alınır.
                userDtos[i].Roles = roles; // dto'ya roller atanır.
            }

            return userDtos;
        }

        public async Task UpdateRoleAsync(AppRoleUpdateDto dto, CancellationToken cancellationToken = default)
        {
            var existingRole = await _roleManager.FindByIdAsync(dto.Id.ToString());
            if (existingRole == null)
                throw new LogicException("RoleNotFound", "Güncellenmek istenen rol sistemde bulunamadı.");

            var isNameChanged = existingRole.Name != dto.Name;
            var isDeactivated = !existingRole.IsDeleted && dto.IsDeleted;

            // sistem rollerinin (Admin, Customer) sadece açıklaması güncellenebilir; adı değiştirilemez ve pasife alınamaz.
            if (IsSystemRole(existingRole) && (isNameChanged || isDeactivated))
                throw new LogicException("ProtectedRole", $"{existingRole.Name} rolü sistem rolüdür; adı değiştirilemez ve pasife alınamaz.");

            if (isNameChanged)
            {
                var roleWithSameName = await _roleManager.FindByNameAsync(dto.Name!);
                if (roleWithSameName != null)
                    throw new LogicException("RoleExist", "Bu rol adı zaten sistemde başka bir rol tarafından kullanılmaktadır. Lütfen farklı bir isim belirleyin.");
            }

            // rol adı token'a gömüldüğü için ad değişirse veya rol pasife alınırsa bu roldeki kullanıcıların oturumları sonlandırılır.
            var usersToRevoke = isNameChanged || isDeactivated
                ? await _userManager.GetUsersInRoleAsync(existingRole.Name!)
                : new List<AppUser>();

            _mapper.Map(dto, existingRole);

            var result = await _roleManager.UpdateAsync(existingRole);
            if (!result.Succeeded)
            {
                var errors = string.Join(" | ", result.Errors.Select(e => e.Description));
                throw new LogicException("RoleUpdateFailed", errors);
            }

            await RevokeSessionsAsync(usersToRevoke);
        }

        private static bool IsSystemRole(AppRole role)
            => RoleNames.SystemRoles.Contains(role.Name, StringComparer.OrdinalIgnoreCase);

        // kullanıcıların refresh token'ı silinir ve security stamp yenilenir; mevcut access token'lar anında geçersiz olur (Program.cs -> OnTokenValidated).
        private async Task RevokeSessionsAsync(IEnumerable<AppUser> users)
        {
            foreach (var user in users)
            {
                user.RefreshToken = null;
                user.RefreshTokenExpiryTime = null;
                await _userManager.UpdateSecurityStampAsync(user); // stamp'i yeniler ve kullanıcıyı (refresh token değişikliğiyle birlikte) kaydeder.
            }
        }
    }
}
