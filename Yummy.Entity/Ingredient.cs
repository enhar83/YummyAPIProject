using System;
using System.Collections.Generic;
using Yummy.Entity.Enums;

namespace Yummy.Entity
{
    // stok kartı. kartları ve stoğu çalışanlar (Employee) yönetir; şefler sadece görüntüler.
    public class Ingredient : BaseEntity
    {
        public Guid IngredientId { get; set; }
        public string Name { get; set; } = null!;
        public IngredientUnit Unit { get; set; }

        // stok sadece hareket kaydıyla birlikte değişir (IngredientManager.AdjustStockAsync); kart güncellemesi bu alana dokunmaz.
        public decimal StockQuantity { get; set; }

        public ICollection<StockMovement> StockMovements { get; set; } = new HashSet<StockMovement>();
    }
}
