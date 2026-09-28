using System;
using Yummy.Entity.Enums;

namespace Yummy.Entity
{
    // talepteki bir malzeme satırı. talebin parçası olduğu için ayrıca silinmez (BaseEntity'den türemez); talep ile birlikte yaşar.
    public class IngredientRequestItem
    {
        public Guid IngredientRequestItemId { get; set; }

        public Guid IngredientRequestId { get; set; }
        public IngredientRequest IngredientRequest { get; set; } = null!;

        public Guid IngredientId { get; set; }
        public Ingredient Ingredient { get; set; } = null!;

        // talep anındaki malzeme adı ve birimi. malzeme kartı sonradan silinse veya adı değişse bile geçmiş talep doğru okunur.
        public string IngredientName { get; set; } = null!;
        public IngredientUnit Unit { get; set; }

        public decimal RequestedQuantity { get; set; }

        // çalışanın gerçekte tedarik ettiği miktar (istenenden az veya fazla olabilir; 0 = bu malzeme bulunamadı). tedarik edilene kadar boştur.
        public decimal? SuppliedQuantity { get; set; }
    }
}
