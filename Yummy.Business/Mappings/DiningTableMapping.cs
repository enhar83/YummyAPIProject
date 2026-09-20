using AutoMapper;
using Yummy.Core.DTOs.DiningTableDTOs;
using Yummy.Entity;

namespace Yummy.Business.Mappings
{
    public class DiningTableMapping : Profile
    {
        public DiningTableMapping()
        {
            CreateMap<DiningTable, DiningTableListDto>().ReverseMap();
            CreateMap<DiningTable, DiningTableCreateDto>().ReverseMap();
            CreateMap<DiningTable, DiningTableUpdateDto>().ReverseMap();
        }
    }
}
