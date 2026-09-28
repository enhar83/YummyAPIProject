using Yummy.Core.DTOs.CommonDTOs;
using Yummy.Core.DTOs.IngredientRequestDTOs;

namespace Yummy.Core.Services
{
    // şefin malzeme talepleri. şef talep oluşturur / iptal eder; çalışan (Employee) tedarik eder veya reddeder.
    public interface IIngredientRequestService
    {
        // şef tarafı (userId: token'daki kullanıcı; şef profiline bağlı olmalıdır).
        Task<Guid> CreateAsync(string userId, IngredientRequestCreateDto dto, CancellationToken cancellationToken = default);
        Task<PagedResultDto<IngredientRequestListDto>> GetMyRequestsAsync(string userId, IngredientRequestQueryDto query, CancellationToken cancellationToken = default);
        Task<IngredientRequestListDto> GetMyRequestByIdAsync(string userId, Guid requestId, CancellationToken cancellationToken = default);
        Task CancelAsync(string userId, Guid requestId, CancellationToken cancellationToken = default);

        // çalışan tarafı (userId: talebi sonuçlandıran çalışan).
        Task<PagedResultDto<IngredientRequestListDto>> GetAllAsync(IngredientRequestQueryDto query, CancellationToken cancellationToken = default);
        Task<IngredientRequestListDto> GetByIdAsync(Guid requestId, CancellationToken cancellationToken = default);
        Task SupplyAsync(string userId, Guid requestId, IngredientRequestSupplyDto dto, CancellationToken cancellationToken = default);
        Task RejectAsync(string userId, Guid requestId, IngredientRequestRejectDto dto, CancellationToken cancellationToken = default);
    }
}
