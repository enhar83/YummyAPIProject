namespace Yummy.Core.DTOs.CommonDTOs
{
    // listeleme endpoint'lerinde query string'den alınan sayfalama parametreleri. (örn. ?page=2&pageSize=20)
    public class PaginationQueryDto
    {
        public const int MaxPageSize = 100;

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
