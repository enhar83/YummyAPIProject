using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yummy.Core.DTOs.AppRoleDTOs
{
    public class AppRoleCreateDto
    {
        public string Name { get; init; } = null!;
        public string Description { get; init; } = null!;
    }
}
