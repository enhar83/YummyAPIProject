using System.Threading;
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yummy.Entity;

namespace Yummy.Core.Services
{
    public interface IJwtService
    {
        (string Token, DateTime ExpiresAt) CreateToken(AppUser user, IEnumerable<string> roles);
        Task<string?> GetUserIdFromExpiredTokenAsync(string accessToken);
    }
}
