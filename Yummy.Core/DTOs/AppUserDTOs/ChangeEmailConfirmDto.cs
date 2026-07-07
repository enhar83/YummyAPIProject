using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yummy.Core.DTOs.AppUserDTOs
{
    public class ChangeEmailConfirmDto
    {
        public string NewEmail { get; set; } = null!;
        public string Token { get; set; } = null!;
    }
}
