using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Yummy.Core.DTOs.ContactDTOs;
using Yummy.Core.Exceptions;
using Yummy.Core.IRepositories;
using Yummy.Core.IUnitOfWork;
using Yummy.Core.Services;
using Yummy.Entity;

namespace Yummy.Business.Managers
{
    public class ContactManager : IContactService
    {
        private readonly IGenericRepository<Contact> _contactRepository;
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public ContactManager(IGenericRepository<Contact> contactRepository, IUnitOfWork uow, IMapper mapper)
        {
            _contactRepository = contactRepository;
            _uow = uow;
            _mapper = mapper;
        }

        public async Task AddAsync(ContactCreateDto dto)
        {
            var contact = _mapper.Map<Contact>(dto);
            bool isContactExist = await _contactRepository.AnyAsync(c => c.Address == dto.Address && c.Email == dto.Email && c.Phone == dto.Phone);
            if (isContactExist)
                throw new LogicException("ContactId", "Bu iletişim bilgileri zaten kullanılıyor.");

            contact.ContactId = Guid.NewGuid();
            await _contactRepository.AddAsync(contact);
            await _uow.SaveAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var contact = await _contactRepository.GetByIdAsync(id);
            if (contact == null)
                throw new LogicException("ContactId", "Bu iletişim bilgileri bulunamadı.");

            _contactRepository.Remove(contact);
            await _uow.SaveAsync();
        }

        public async Task<IEnumerable<ContactResponseDto>> GetAllAsync()
        {
            return await _contactRepository.GetAsQueryable()
                .ProjectTo<ContactResponseDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }

        public async Task<ContactResponseDto?> GetByIdAsync(Guid id)
        {
            var contact = await _contactRepository.GetByIdAsync(id);
            if (contact == null)
                throw new LogicException("ContactId", "Bu iletişim bilgileri bulunamadı.");

            return _mapper.Map<ContactResponseDto>(contact);
        }

        public async Task UpdateAsync(ContactUpdateDto dto)
        {
            var contact = await _contactRepository.GetByIdAsync(dto.ContactId);
            if (contact == null)
                throw new LogicException("ContactId", "Bu iletişim bilgileri bulunamadı.");

            bool isContactExist = await _contactRepository.AnyAsync(c => c.Address == dto.Address &&
                c.Email == dto.Email &&
                c.Phone == dto.Phone &&
                c.ContactId != dto.ContactId);
            if (isContactExist)
                throw new LogicException("ContactId", "Bu iletişim bilgileri zaten kullanılıyor.");

            _mapper.Map(dto, contact);
            _contactRepository.Update(contact);
            await _uow.SaveAsync();
        }
    }
}
