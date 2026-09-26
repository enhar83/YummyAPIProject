using System;
using Microsoft.AspNetCore.Identity;

namespace Yummy.Entity
{
    public class AppRole : IdentityRole<Guid>
    {
        public required string Description { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }
    }
}
