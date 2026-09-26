using System.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yummy.Core.DTOs.CategoryDTOs;
using Yummy.Core.DTOs.ChefDTOs;

namespace Yummy.Core.Services
{
    public interface IChefService
    {
        Task<IEnumerable<ChefResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<ChefResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task AddAsync(ChefCreateDto dto, CancellationToken cancellationToken = default);
        Task UpdateAsync(ChefUpdateDto dto, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
