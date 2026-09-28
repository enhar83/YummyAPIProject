using Yummy.Entity.Enums;

namespace Yummy.Core.DTOs.IngredientRequestDTOs
{
    public record IngredientRequestListDto
    {
        public Guid IngredientRequestId { get; init; }
        public IngredientRequestStatus Status { get; init; }
        public string StatusName { get; init; } = null!; // ekranda gösterim için (örn. "Bekliyor")
        public string ChefName { get; init; } = null!;
        public string? ChefNote { get; init; }
        public string? ResponseNote { get; init; }
        public string? HandledBy { get; init; } // talebi sonuçlandıran çalışanın adı soyadı
        public DateTime? HandledDate { get; init; }
        public DateTime CreatedDate { get; init; }
        public List<IngredientRequestItemListDto> Items { get; init; } = new();
    }

    public record IngredientRequestItemListDto
    {
        public Guid IngredientRequestItemId { get; init; }
        public Guid IngredientId { get; init; }
        public string IngredientName { get; init; } = null!;
        public IngredientUnit Unit { get; init; }
        public string UnitName { get; init; } = null!;
        public decimal RequestedQuantity { get; init; }
        public decimal? SuppliedQuantity { get; init; }
    }
}
