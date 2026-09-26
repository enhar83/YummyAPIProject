using System.Threading;
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
        Task<IEnumerable<ContactResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<ContactResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task AddAsync(ContactCreateDto dto, CancellationToken cancellationToken = default);
        Task UpdateAsync(ContactUpdateDto dto, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
