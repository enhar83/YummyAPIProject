using Yummy.Entity.Enums;

namespace Yummy.Core.DTOs.IngredientDTOs
{
    // çalışanın elle yaptığı stok hareketi. /// açıklamaları Swagger'da alan açıklaması olarak görünür.
    public record StockAdjustmentDto
    {
        /// <summary>
        /// Stok hareketinin türü:
        /// StockIn = stok girişi, quantity kadar stoğa eklenir (örn. alım geldi).
        /// Waste = fire, quantity kadar stoktan düşülür (örn. bozuldu, döküldü). Stoktaki miktardan fazla fire girilemez.
        /// CountCorrection = sayım düzeltmesi, stok quantity değerine eşitlenir (quantity = sayılan gerçek miktar; 0 girilebilir).
        /// </summary>
        public StockMovementType Type { get; init; }

        /// <summary>
        /// Malzemenin kendi birimiyle miktar (örn. kg ise 2.5 = 2,5 kg). En fazla 3 ondalık basamak.
        /// StockIn ve Waste için 0'dan büyük olmalıdır.
        /// </summary>
        public decimal Quantity { get; init; }

        /// <summary>
        /// İsteğe bağlı açıklama (örn. "Haftalık alım", "Soğuk zincir bozuldu"). En fazla 250 karakter.
        /// </summary>
        public string? Note { get; init; }
    }
}
