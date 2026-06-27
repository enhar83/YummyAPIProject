using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yummy.Core.DTOs.FeatureDTOs;

namespace Yummy.Core.Services
{
    public interface IFeatureService
    {
        Task<IEnumerable<FeatureResponseDto>> GetAllAsync();
        Task<FeatureResponseDto?> GetByIdAsync(Guid id);
        Task AddAsync(FeatureCreateDto dto);
        Task UpdateAsync(FeatureUpdateDto dto);
        Task DeleteAsync(Guid id);
    }
}
