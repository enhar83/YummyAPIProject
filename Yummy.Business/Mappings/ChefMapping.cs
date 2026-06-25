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
            CreateMap<Chef, ChefResponseDto>().ReverseMap();
            CreateMap<ChefCreateDto, Chef>();
            CreateMap<ChefUpdateDto, Chef>();
        }
    }
}
