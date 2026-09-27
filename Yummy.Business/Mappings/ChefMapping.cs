using System.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Yummy.Core.DTOs.ChefDTOs;
using Yummy.Entity;

namespace Yummy.Business.Mappings
{
    public class ChefMapping : Profile
    {
        public ChefMapping()
        {
            // ReverseMap kullanılmaz: DTO'dan entity'ye dönüşte AppUserId gibi admin'in ayrı endpoint ile yönettiği alanlar ezilmemelidir.
            CreateMap<Chef, ChefResponseDto>()
                .ForMember(dest => dest.LinkedUserEmail, opt => opt.MapFrom(src => src.AppUser != null ? src.AppUser.Email : null));

            CreateMap<ChefCreateDto, Chef>()
                .ForMember(dest => dest.ImageUrl, opt => opt.Ignore());

            CreateMap<ChefUpdateDto, Chef>()
                .ForMember(dest => dest.ImageUrl, opt => opt.Ignore());
        }
    }
}
