using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yummy.Entity;

namespace Yummy.Core.Services
{
    public interface IJwtService
    {
        string CreateToken(AppUser user, IEnumerable<string> roles);
    }
}
