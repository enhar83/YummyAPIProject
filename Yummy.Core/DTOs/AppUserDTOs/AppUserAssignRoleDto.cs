using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yummy.Core.DTOs.AppUserDTOs
{
    public class AppUserAssignRoleDto
    {
        public Guid UserId { get; set; }
        public IList<string> RoleNames { get; set; } = new List<string>();
    }
}
