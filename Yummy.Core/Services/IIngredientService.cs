using Yummy.Core.DTOs.CommonDTOs;
using Yummy.Core.DTOs.IngredientDTOs;

namespace Yummy.Core.Services
{
    // stok kartları ve stok hareketleri. yazma işlemleri çalışanlar (Employee) ve admin içindir; şefler listeyi sadece görüntüler.
    public interface IIngredientService
    {
        Task<IEnumerable<IngredientListDto>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IngredientListDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task AddAsync(IngredientCreateDto dto, CancellationToken cancellationToken = default);
        Task UpdateAsync(IngredientUpdateDto dto, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

        Task AdjustStockAsync(string userId, Guid ingredientId, StockAdjustmentDto dto, CancellationToken cancellationToken = default);
        Task<PagedResultDto<StockMovementListDto>> GetStockMovementsAsync(Guid ingredientId, PaginationQueryDto query, CancellationToken cancellationToken = default);
    }
}
