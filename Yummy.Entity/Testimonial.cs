using System;
using System.ComponentModel.DataAnnotations;

namespace Yummy.Entity
{
    public class Testimonial : BaseEntity
    {
        public Guid TestimonialId { get; set; }
        public string Title { get; set; } = null!;
        public string Comment { get; set; } = null!;

        /// <summary>
        /// 1 ile 5 arasında puan. FluentAPI ile [1,5] aralığı veritabanı seviyesinde de kısıtlanır.
        /// </summary>
        [Range(1, 5)]
        public byte Rating { get; set; }

        public bool IsApproved { get; set; } = false;
        public Guid AppUserId { get; set; }
        public AppUser AppUser { get; set; } = null!;
    }
}
