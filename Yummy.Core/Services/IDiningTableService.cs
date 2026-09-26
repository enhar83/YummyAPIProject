using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Yummy.Core.DTOs.DiningTableDTOs;

namespace Yummy.Core.Services
{
    // masa silme işlemi bilinçli olarak yoktur; masalar UpdateAsync ile pasife alınır (bkz. DiningTableManager).
    public interface IDiningTableService
    {
        Task<IEnumerable<DiningTableListDto>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<DiningTableListDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task AddAsync(DiningTableCreateDto dto, CancellationToken cancellationToken = default);
        Task UpdateAsync(DiningTableUpdateDto dto, CancellationToken cancellationToken = default);
    }
}
