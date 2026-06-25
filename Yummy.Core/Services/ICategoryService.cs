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
        Task<IEnumerable<CategoryResponseDto>> GetAllAsync();
        Task<CategoryResponseDto?> GetByIdAsync(Guid id);
        Task AddAsync(CategoryCreateDto categoryCreateDto);
        Task UpdateAsync(CategoryUpdateDto categoryUpdateDto);
        Task DeleteAsync(Guid id);
    }
}
