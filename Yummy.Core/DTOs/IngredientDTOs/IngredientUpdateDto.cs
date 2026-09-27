using Yummy.Entity.Enums;

namespace Yummy.Core.DTOs.IngredientDTOs
{
    // sadece kart bilgileri güncellenir. stok miktarı bu DTO ile değiştirilemez (bkz. StockAdjustmentDto).
    public record IngredientUpdateDto
    {
        public Guid IngredientId { get; init; }

        /// <summary>
        /// Malzeme adı. Büyük/küçük harf farkı gözetmeksizin benzersiz olmalıdır. En fazla 100 karakter.
        /// </summary>
        public string Name { get; set; } = null!;

        /// <summary>
        /// Stoğun hangi birimle tutulduğu: Gram, Kilogram, Milliliter (mililitre), Liter (litre), Piece (adet).
        /// Stok hareketi olan malzemenin birimi değiştirilemez.
        /// </summary>
        public IngredientUnit Unit { get; init; }
    }
}
