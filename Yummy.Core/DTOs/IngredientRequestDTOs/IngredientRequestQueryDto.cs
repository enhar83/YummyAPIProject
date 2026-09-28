using Yummy.Core.DTOs.CommonDTOs;
using Yummy.Entity.Enums;

namespace Yummy.Core.DTOs.IngredientRequestDTOs
{
    // talep listesi: sayfalama + isteğe bağlı durum filtresi. (örn. ?status=Pending&page=1&pageSize=20)
    public class IngredientRequestQueryDto : PaginationQueryDto
    {
        /// <summary>
        /// Sadece bu durumdaki talepler: Pending (bekleyen), Supplied (tedarik edildi), Rejected (reddedildi), Cancelled (iptal edildi). Boş = hepsi.
        /// </summary>
        public IngredientRequestStatus? Status { get; set; }
    }
}
