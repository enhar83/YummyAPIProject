using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yummy.Core.DTOs.AppUserDTOs
{
    public class AppUserRegisterDto
    {
        public string Name { get; init; } = null!;
        public string Surname { get; init; } = null!;
        public string Username { get; init; } = null!;
        public string Email { get; init; } = null!;
        public string Password { get; init; } = null!;
        public string ConfirmPassword { get; init; } = null!;
    }
}
