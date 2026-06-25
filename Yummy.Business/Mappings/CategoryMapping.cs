using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Yummy.Core.DTOs.CategoryDTOs;
using Yummy.Entity;

namespace Yummy.Business.Mapping
{
    public class CategoryMapping : Profile
    {
        public CategoryMapping() 
        {
            CreateMap<Category, CategoryResponseDto>().ReverseMap();
            CreateMap<Category, CategoryCreateDto>().ReverseMap();
            CreateMap<Category, CategoryUpdateDto>().ReverseMap();
        }
    }
}
