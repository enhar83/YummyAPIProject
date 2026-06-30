using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Yummy.Core.DTOs.AppRoleDTOs;
using Yummy.Core.Exceptions;
using Yummy.Core.Services;
using Yummy.Entity;

namespace Yummy.Business.Managers
{
    public class AppRoleManager : IAppRoleService
    {
        private readonly RoleManager<AppRole> _roleManager;
        private readonly IMapper _mapper;

        public AppRoleManager(RoleManager<AppRole> roleManager, IMapper mapper)
        {
            _roleManager = roleManager;
            _mapper = mapper;
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
    }
}
