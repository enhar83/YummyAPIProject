using Yummy.Entity.Enums;

namespace Yummy.Core.DTOs.IngredientDTOs
{
    public record IngredientListDto
    {
        public Guid IngredientId { get; init; }
        public string Name { get; init; } = null!;
        public IngredientUnit Unit { get; init; }
        public string UnitName { get; init; } = null!; // ekranda gösterim için (örn. "kg")
        public decimal StockQuantity { get; init; }
        public DateTime? UpdatedDate { get; init; }
    }
}
