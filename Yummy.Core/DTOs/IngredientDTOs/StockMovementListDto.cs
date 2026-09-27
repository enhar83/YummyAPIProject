using Yummy.Entity.Enums;

namespace Yummy.Core.DTOs.IngredientDTOs
{
    public record StockMovementListDto
    {
        public Guid StockMovementId { get; init; }
        public StockMovementType Type { get; init; }
        public string TypeName { get; init; } = null!;
        public decimal QuantityChange { get; init; }
        public decimal QuantityAfter { get; init; }
        public string? Note { get; init; }
        public string? PerformedBy { get; init; } // hareketi yapan kullanıcının adı soyadı
        public DateTime CreatedDate { get; init; }
    }
}
