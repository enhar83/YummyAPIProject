using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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

        public async Task CreateRoleAsync(AppRoleCreateDto dto)
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

        public async Task DeleteRoleAsync(Guid id)
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null)
                throw new LogicException("RoleNotFound", "Silinmek istenen rol sistemde bulunamadı.");

            var result = await _roleManager.DeleteAsync(role);
            if (!result.Succeeded)
            {
                var errors = string.Join(" | ", result.Errors.Select(e => e.Description));
                throw new LogicException("RoleDeleteFailed", errors);
            }
        }

        public async Task<IEnumerable<AppRoleListDto>> GetAllRolesAsync()
        {
            var roles = await _roleManager.Roles
                .ProjectTo<AppRoleListDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            return roles;
        }

        public async Task<IEnumerable<AppUserListDto>> GetAllUsersInRoleAsync(Guid roleId)
        {
            var role = await _roleManager.FindByIdAsync(roleId.ToString());
            if (role == null)
                throw new LogicException("RoleNotFound", "Böyle bir rol bulunamadı.");

            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
            var userDtos = _mapper.Map<List<AppUserListDto>>(usersInRole);

            for (int i = 0; i < usersInRole.Count; i++)
            {
                var roles = await _userManager.GetRolesAsync(usersInRole[i]);
                userDtos[i].Roles = roles;
            }

            return userDtos;
        }

        public async Task UpdateRoleAsync(AppRoleUpdateDto dto)
        {
            var existingRole = await _roleManager.FindByIdAsync(dto.Id.ToString());
            if (existingRole == null)
                throw new LogicException("RoleNotFound", "Güncellenmek istenen rol sistemde bulunamadı.");

            if (existingRole.Name != dto.Name)
            {
                var roleWithSameName = await _roleManager.FindByNameAsync(dto.Name!);
                if (roleWithSameName != null)
                    throw new LogicException("RoleExist", "Bu rol adı zaten sistemde başka bir rol tarafından kullanılmaktadır. Lütfen farklı bir isim belirleyin.");
            }

            _mapper.Map(dto, existingRole);

            var result = await _roleManager.UpdateAsync(existingRole);
            if (!result.Succeeded)
            {
                var errors = string.Join(" | ", result.Errors.Select(e => e.Description));
                throw new LogicException("RoleUpdateFailed", errors);
            }
        }
    }
}
