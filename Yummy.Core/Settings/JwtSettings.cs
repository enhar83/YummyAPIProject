using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yummy.Core.Settings
{
    public class JwtSettings
    {
        public string Audience { get; init; } = null!;
        public string Issuer { get; init; } = null!;   
        public int AccessTokenExpiration { get; init; } 
        public string SecurityKey { get; init; } = null!;
    }
}
