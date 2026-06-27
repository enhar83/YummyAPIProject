using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yummy.Core.DTOs.ContactDTOs;

namespace Yummy.Core.Services
{
    public interface IContactService
    {
        Task<IEnumerable<ContactResponseDto>> GetAllAsync();
        Task<ContactResponseDto?> GetByIdAsync(Guid id);
        Task AddAsync(ContactCreateDto dto);
        Task UpdateAsync(ContactUpdateDto dto);
        Task DeleteAsync(Guid id);
    }
}
