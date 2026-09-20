using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Yummy.Core.Services;
using Yummy.Core.Settings;
using Yummy.Entity;

namespace Yummy.Business.Managers
{
    public class JwtManager : IJwtService
    {
        private readonly JwtSettings _jwtSettings;

        public JwtManager(IOptions<JwtSettings> jwtSettings)
        {
            _jwtSettings = jwtSettings.Value;
        }

        public string CreateToken(AppUser user, IEnumerable<string> roles)
        {
            // token içerisinde herhangi bir değişiklik yapılırsa farkedilebilmesi için şifrelenme vs. işlemleri yapılır.
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecurityKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // claimler token içerisine koyulan kullanıcıya ait olan bilgilerdir.
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            if (!string.IsNullOrEmpty(user.Email))
                claims.Add(new Claim(ClaimTypes.Email, user.Email));

            if (!string.IsNullOrEmpty(user.Name) && !string.IsNullOrEmpty(user.Surname))
                claims.Add(new Claim(ClaimTypes.Name, $"{user.Name} {user.Surname}"));

            if (!string.IsNullOrEmpty(user.UserName))
                claims.Add(new Claim("Username", user.UserName));

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddMinutes(_jwtSettings.AccessTokenExpiration), 
                SigningCredentials = credentials
            };

            var handler = new JsonWebTokenHandler(); // .NET standartlarına uygun olan JsonWebTokenHandler kullanılarak token string formatında oluşturulup döndürülür.
            return handler.CreateToken(tokenDescriptor);
        }
    }
}