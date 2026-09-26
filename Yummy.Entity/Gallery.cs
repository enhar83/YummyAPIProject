using System;

namespace Yummy.Entity
{
    public class Gallery : BaseEntity
    {
        public Guid GalleryId { get; set; }
        public string Title { get; set; } = null!;
        public string ImageUrl { get; set; } = null!;
        /// <summary>
        /// Galeride gösterim sırası. Küçük değer önce gösterilir.
        /// </summary>
        public int DisplayOrder { get; set; } = 0;
        /// <summary>
        /// Görselin aktif olup olmadığı. false ise frontend'de gösterilmez.
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}
