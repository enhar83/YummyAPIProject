namespace Yummy.Core.DTOs.CommonDTOs
{
    // sayfalı liste cevabı. istemci TotalPages ile sayfa numaralarını, TotalCount ile toplam kayıt sayısını gösterebilir.
    public class PagedResultDto<T>
    {
        public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        // istenen sayfa toplam sayfa sayısını aşarsa Items boş döner, TotalCount yine doğru gelir.
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
    }
}
