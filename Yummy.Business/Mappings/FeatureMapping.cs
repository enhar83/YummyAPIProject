using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Yummy.Core.DTOs.FeatureDTOs;
using Yummy.Entity;

namespace Yummy.Business.Mappings
{
    public class FeatureMapping : Profile
    {
        public FeatureMapping()
        {
            CreateMap<Feature, FeatureResponseDto>();

            CreateMap<FeatureCreateDto, Feature>()
                .ForMember(dest => dest.ImageUrl, opt => opt.Ignore());

            CreateMap<FeatureUpdateDto, Feature>()
                .ForMember(dest => dest.ImageUrl, opt => opt.Ignore());
        }
    }
}
