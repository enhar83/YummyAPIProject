using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Yummy.Core.DTOs.TestimonialDTOs;
using Yummy.Entity;

namespace Yummy.Business.Mappings
{
    public class TestimonialMapping : Profile
    {
        public TestimonialMapping()
        {
            CreateMap<TestimonialCreateDto, Testimonial>()
                .ForMember(dest => dest.IsApproved, opt => opt.MapFrom(src => false)) 
                .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => DateTime.Now));

            CreateMap<Testimonial, UsersPastTestimonialsListDto>();

            CreateMap<Testimonial, AllTestimonialListDto>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.AppUser.Name))
                .ForMember(dest => dest.Surname, opt => opt.MapFrom(src => src.AppUser.Surname));
        }
    }
}
