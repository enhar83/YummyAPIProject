using System;
using Yummy.Entity.Enums;

namespace Yummy.Entity
{
    // stok hareket kaydı. kayıtlar değiştirilmez ve silinmez; stoğun geçmişi bu tablodan okunur.
    public class StockMovement : BaseEntity
    {
        public Guid StockMovementId { get; set; }
        public StockMovementType Type { get; set; }

        // stoğa etkisi: giriş için artı, düşüm için eksi.
        public decimal QuantityChange { get; set; }

        // hareketten sonraki stok miktarı. geçmişte herhangi bir anda stoğun ne kadar olduğu tek satırdan okunabilir.
        public decimal QuantityAfter { get; set; }

        public string? Note { get; set; }

        public Guid IngredientId { get; set; }
        public Ingredient Ingredient { get; set; } = null!;

        // hareketi yapan kullanıcı. kullanıcı silinirse hareket kaydı korunur, sadece bağlantı kopar.
        public Guid? PerformedByUserId { get; set; }
        public AppUser? PerformedByUser { get; set; }

        // hareket bir malzeme talebinin tedarikiyle oluştuysa ilgili talep (RequestSupply). elle yapılan hareketlerde boştur.
        public Guid? IngredientRequestId { get; set; }
        public IngredientRequest? IngredientRequest { get; set; }
    }
}
