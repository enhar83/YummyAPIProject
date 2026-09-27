using Yummy.Entity.Enums;

namespace Yummy.Core.DTOs.IngredientDTOs
{
    // yeni stok kartı. stok 0 ile başlar; ilk miktar stok girişi (StockIn) ile eklenir, böylece her miktar bir hareket kaydıyla gelir.
    public record IngredientCreateDto
    {
        /// <summary>
        /// Malzeme adı (örn. "Domates"). Büyük/küçük harf farkı gözetmeksizin benzersiz olmalıdır. En fazla 100 karakter.
        /// </summary>
        public string Name { get; set; } = null!;

        /// <summary>
        /// Stoğun hangi birimle tutulacağı: Gram, Kilogram, Milliliter (mililitre), Liter (litre), Piece (adet).
        /// Bu alan miktar değildir; kart 0 stok ile oluşur, miktar stok girişi (stock-adjustments, StockIn) ile eklenir.
        /// </summary>
        public IngredientUnit Unit { get; init; }
    }
}
