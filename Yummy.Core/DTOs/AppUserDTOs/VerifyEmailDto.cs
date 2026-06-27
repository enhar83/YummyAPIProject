using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yummy.Core.DTOs.AppUserDTOs
{
    public class VerifyEmailDto
    {
        public string Email { get; init; } = null!;
        public string ActivationCode { get; init; } = null!;
    }
}
