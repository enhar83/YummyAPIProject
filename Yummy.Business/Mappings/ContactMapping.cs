using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Yummy.Core.DTOs.CategoryDTOs;
using Yummy.Core.DTOs.ContactDTOs;
using Yummy.Entity;

namespace Yummy.Business.Mappings
{
    public class ContactMapping : Profile
    {
        public ContactMapping()
        {
            CreateMap<Contact, ContactResponseDto>();
            CreateMap<ContactCreateDto, Contact>();
            CreateMap<ContactUpdateDto, Contact>();
        }
    }
}
