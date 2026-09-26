using System;

namespace Yummy.Entity
{
    /// <summary>
    /// Tüm entity'lerin ortak alanlarını barındıran temel soyut sınıf.
    /// Soft delete, oluşturma ve güncellenme tarihlerini merkezi olarak yönetir.
    /// </summary>
    public abstract class BaseEntity
    {
        /// <summary>
        /// Kayıt oluşturulduğu tarih. Otomatik olarak o anki tarih atanır.
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Kaydın son güncellendiği tarih. Güncelleme yapılmamışsa null olur.
        /// </summary>
        public DateTime? UpdatedDate { get; set; }

        /// <summary>
        /// Soft delete bayrağı. true ise kayıt silinmiş sayılır, veritabanından fiziksel olarak kaldırılmaz.
        /// </summary>
        public bool IsDeleted { get; set; } = false;
    }
}
