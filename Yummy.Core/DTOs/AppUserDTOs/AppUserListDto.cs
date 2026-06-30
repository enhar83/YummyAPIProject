using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yummy.Core.DTOs.AppUserDTOs
{
    public class AppUserListDto
    {
        public Guid Id { get; init; }
        public string FullName { get; init; } = null!;
        public string Email { get; init; } = null!;
        public string UserName { get; init; } = null!;
        public string PhoneNumber { get; init; } = null!;
        public bool EmailConfirmed { get; init; }
        public string? ImageUrl { get; init; }
        public IEnumerable<string> Roles { get; set; } = Enumerable.Empty<string>();
        public bool IsDeleted { get; init; }
        public DateTime CreatedDate { get; init; }
    }
}
