using System.Threading;
﻿using System;
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

        public async Task AddAsync(ContactCreateDto dto, CancellationToken cancellationToken = default)
        {
            var contact = _mapper.Map<Contact>(dto);
            bool isContactExist = await _contactRepository.AnyAsync(c => c.Address == dto.Address && c.Email == dto.Email && c.Phone == dto.Phone, cancellationToken);
            if (isContactExist)
                throw new LogicException("ContactId", "Bu iletişim bilgileri zaten kullanılıyor.");

            contact.ContactId = Guid.NewGuid();
            await _contactRepository.AddAsync(contact, cancellationToken);
            await _uow.SaveAsync(cancellationToken);
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var contact = await _contactRepository.GetByIdAsync(id, cancellationToken);
            if (contact == null)
                throw new LogicException("ContactId", "Bu iletişim bilgileri bulunamadı.");

            _contactRepository.Remove(contact);
            await _uow.SaveAsync(cancellationToken);
        }

        public async Task<IEnumerable<ContactResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var entities = await _contactRepository.GetAllAsync(cancellationToken);
            return _mapper.Map<IEnumerable<ContactResponseDto>>(entities);
        }

        public async Task<ContactResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var contact = await _contactRepository.GetByIdAsync(id, cancellationToken);
            if (contact == null)
                throw new LogicException("ContactId", "Bu iletişim bilgileri bulunamadı.");

            return _mapper.Map<ContactResponseDto>(contact);
        }

        public async Task UpdateAsync(ContactUpdateDto dto, CancellationToken cancellationToken = default)
        {
            var contact = await _contactRepository.GetByIdAsync(dto.ContactId, cancellationToken);
            if (contact == null)
                throw new LogicException("ContactId", "Bu iletişim bilgileri bulunamadı.");

            bool isContactExist = await _contactRepository.AnyAsync(c => c.Address == dto.Address &&
                c.Email == dto.Email &&
                c.Phone == dto.Phone &&
                c.ContactId != dto.ContactId, cancellationToken);
            if (isContactExist)
                throw new LogicException("ContactId", "Bu iletişim bilgileri zaten kullanılıyor.");

            _mapper.Map(dto, contact);
            _contactRepository.Update(contact);
            await _uow.SaveAsync(cancellationToken);
        }
    }
}
