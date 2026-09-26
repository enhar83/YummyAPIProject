using System;

namespace Yummy.Entity
{
    public class Product : BaseEntity
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public string ProductDescription { get; set; } = null!;
        public decimal Price { get; set; }
        public string ImageUrl { get; set; } = null!;
        /// <summary>
        /// Ürünün menüde aktif olup olmadığı. false ise mevsimlik/geçici ürünler devre dışı bırakılabilir.
        /// </summary>
        public bool IsActive { get; set; } = true;
        public Guid CategoryId { get; set; }
        public Category Category { get; set; } = null!;
    }
}
