using System;
using System.Collections.Generic;

namespace Yummy.Entity
{
    public class Chef : BaseEntity
    {
        public Guid ChefId { get; set; }
        public string Name { get; set; } = null!;
        public string Surname { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string ImageUrl { get; set; } = null!;
    }
}
