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

            CreateMap<ReservationUpdateDto, Reservation>()
                .ForMember(dest => dest.Message, opt => opt.MapFrom(src => src.Message ?? string.Empty))
                .ForMember(dest => dest.AppUserId, opt => opt.Ignore());

            CreateMap<Reservation, PastReservationByUserDto>()
                .ForMember(dest => dest.ReservationStatus, opt => opt.MapFrom(src =>
                    src.ReservationStatus == ReservationStatus.Pending ? "Onay Bekliyor" :
                    src.ReservationStatus == ReservationStatus.Approved ? "Onaylandı" :
                    src.ReservationStatus == ReservationStatus.Completed ? "Tamamlandı" :
                    src.ReservationStatus == ReservationStatus.Cancelled ? "İptal Edildi" : "Bilinmiyor"
                ));

            CreateMap<Reservation, ReservationListDto>()
                .ForMember(dest => dest.ReservationStatus, opt => opt.MapFrom(src =>
                    src.ReservationStatus == ReservationStatus.Pending ? "Onay Bekliyor" :
                    src.ReservationStatus == ReservationStatus.Approved ? "Onaylandı" :
                    src.ReservationStatus == ReservationStatus.Completed ? "Tamamlandı" :
                    src.ReservationStatus == ReservationStatus.Cancelled ? "İptal Edildi" : "Bilinmiyor"
                ));
        }
    }
}
