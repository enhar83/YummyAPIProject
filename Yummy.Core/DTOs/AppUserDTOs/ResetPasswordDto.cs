using System.Threading;
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yummy.Core.DTOs.AppUserDTOs
{
    public class ResetPasswordDto
    {
        public string Email { get; init; } = null!;
        public string Token { get; init; } = null!;
        public string NewPassword { get; init; } = null!;
        public string ConfirmNewPassword { get; init; } = null!;
    }
}
