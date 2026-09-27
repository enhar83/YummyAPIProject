using Yummy.Entity.Enums;

namespace Yummy.Core.DTOs.IngredientDTOs
{
    // yeni stok kartı. stok 0 ile başlar; ilk miktar stok girişi (StockIn) ile eklenir, böylece her miktar bir hareket kaydıyla gelir.
    public record IngredientCreateDto
    {
        public string Name { get; set; } = null!;
        public IngredientUnit Unit { get; init; }
    }
}
