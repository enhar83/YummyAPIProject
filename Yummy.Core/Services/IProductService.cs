using System.Threading;
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yummy.Core.DTOs.ChefDTOs;
using Yummy.Core.DTOs.ProductDTOs;

namespace Yummy.Core.Services
{
    public interface IProductService
    {
        Task<IEnumerable<ProductResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<ProductResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task AddAsync(ProductCreateDto dto, CancellationToken cancellationToken = default);
        Task UpdateAsync(ProductUpdateDto dto, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
