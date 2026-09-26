using System.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yummy.Core.DTOs.CategoryDTOs;
using Yummy.Entity;

namespace Yummy.Core.Services
{
    public interface ICategoryService
    {
        Task<IEnumerable<CategoryResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<CategoryResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task AddAsync(CategoryCreateDto dto, CancellationToken cancellationToken = default);
        Task UpdateAsync(CategoryUpdateDto dto, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
