using Yummy.Entity.Enums;

namespace Yummy.Core.DTOs.IngredientDTOs
{
    // çalışanın elle yaptığı stok hareketi.
    // StockIn: Quantity kadar eklenir. Waste: Quantity kadar düşülür. CountCorrection: stok, sayılan miktar olan Quantity'ye eşitlenir.
    public record StockAdjustmentDto
    {
        public StockMovementType Type { get; init; }
        public decimal Quantity { get; init; }
        public string? Note { get; init; }
    }
}
