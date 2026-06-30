using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Yummy.Core.DTOs.AppRoleDTOs;
using Yummy.Entity;

namespace Yummy.Business.Mappings
{
    public class AppRoleMapping : Profile
    {
        public AppRoleMapping() 
        {
            CreateMap<AppRoleCreateDto, AppRole>();
        }
    }
}
