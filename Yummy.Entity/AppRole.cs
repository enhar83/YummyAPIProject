using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace Yummy.Entity
{
    public class AppRole:IdentityRole<Guid>
    {
        public required string Description { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
