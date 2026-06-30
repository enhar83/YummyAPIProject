using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yummy.Core.DTOs.AppRoleDTOs
{
    public class AppRoleUpdateDto
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = null!;
        public string Description { get; init; } = null!;
        public bool IsDeleted { get; init; }
    }
}
