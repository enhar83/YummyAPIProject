using Yummy.Entity.Enums;

namespace Yummy.Core.DTOs.IngredientDTOs
{
    // sadece kart bilgileri güncellenir. stok miktarı bu DTO ile değiştirilemez (bkz. StockAdjustmentDto).
    public record IngredientUpdateDto
    {
        public Guid IngredientId { get; init; }
        public string Name { get; set; } = null!;
        public IngredientUnit Unit { get; init; }
    }
}
