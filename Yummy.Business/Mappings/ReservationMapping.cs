using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Yummy.Business.Validators.ReservationValidators;
using Yummy.Core.DTOs.ReservationDTOs;
using Yummy.Entity;
using Yummy.Entity.Enums;

namespace Yummy.Business.Mappings
{
    public class ReservationMapping : Profile
    {
        public ReservationMapping()
        {
            CreateMap<ReservationCreateDto, Reservation>()
                .ForMember(dest => dest.Message, opt => opt.MapFrom(src => src.Message ?? string.Empty))
                .ForMember(dest => dest.ReservationStatus, opt => opt.MapFrom(src => ReservationStatus.Pending))
                .ForMember(dest => dest.AppUserId, opt => opt.Ignore());
        }
    }
}
