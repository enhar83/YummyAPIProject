using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Yummy.Entity;

namespace Yummy.Data.Context
{
    public class YummyDbContext : IdentityDbContext<AppUser, AppRole, Guid>
    {
        public YummyDbContext(DbContextOptions<YummyDbContext> options) : base(options)
        {
        }
    }
}
