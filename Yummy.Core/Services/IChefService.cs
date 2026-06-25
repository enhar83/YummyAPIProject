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
        Task<IEnumerable<ChefResponseDto>> GetAllAsync();
        Task<ChefResponseDto?> GetByIdAsync(Guid id);
        Task AddAsync(ChefCreateDto dto);
        Task UpdateAsync(ChefUpdateDto dto);
        Task DeleteAsync(Guid id);
    }
}
