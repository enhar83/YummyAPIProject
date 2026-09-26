using System;
using System.Collections.Generic;

namespace Yummy.Entity
{
    public class Category : BaseEntity
    {
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = null!;
        public ICollection<Product> Products { get; set; } = new HashSet<Product>(); // bir kategori içerisinde birden fazla ürün bulunabilir.
    }
}
