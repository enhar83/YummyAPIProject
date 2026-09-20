using System.Threading;
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yummy.Core.DTOs.FeatureDTOs;

namespace Yummy.Core.Services
{
    public interface IFeatureService
    {
        Task<IEnumerable<FeatureResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<FeatureResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task AddAsync(FeatureCreateDto dto, CancellationToken cancellationToken = default);
        Task UpdateAsync(FeatureUpdateDto dto, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
