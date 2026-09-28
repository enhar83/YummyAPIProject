using System;
using System.Collections.Generic;
using Yummy.Entity.Enums;

namespace Yummy.Entity
{
    // şefin çalışanlara (Employee) ilettiği malzeme talebi. çalışan tedarik eder (stok artar) veya gerekçesiyle reddeder; şef bilgilendirilir.
    public class IngredientRequest : BaseEntity
    {
        public Guid IngredientRequestId { get; set; }
        public IngredientRequestStatus Status { get; set; } = IngredientRequestStatus.Pending;

        // talep eden şef profili.
        public Guid ChefId { get; set; }
        public Chef Chef { get; set; } = null!;

        // talep anındaki şef adı. şef profili sonradan silinse veya adı değişse bile geçmiş talepler doğru kişiyle listelenir.
        public string ChefName { get; set; } = null!;

        public string? ChefNote { get; set; }

        // çalışanın tedarik/red notu. reddedilen taleplerde zorunludur (şef neden reddedildiğini görür).
        public string? ResponseNote { get; set; }

        // talebi sonuçlandıran çalışan ve zamanı (UTC). şef iptal ettiyse boştur.
        public Guid? HandledByUserId { get; set; }
        public AppUser? HandledByUser { get; set; }
        public DateTime? HandledDate { get; set; }

        public ICollection<IngredientRequestItem> Items { get; set; } = new HashSet<IngredientRequestItem>();
    }
}
