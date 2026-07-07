using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Yummy.Core.DTOs.AppUserDTOs;
using Yummy.Entity;

namespace Yummy.Business.Mappings
{
    public class AppUserMapping : Profile
    {
        public AppUserMapping()
        {
            CreateMap<AppUserRegisterDto, AppUser>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore());

            CreateMap<AppUser, AppUserListDto>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.Name} {src.Surname}"))
                .ForMember(dest => dest.Roles, opt => opt.Ignore());

            CreateMap<UpdateAppUserDto, AppUser>();
        }
    }
}
