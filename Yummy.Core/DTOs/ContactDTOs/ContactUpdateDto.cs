using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yummy.Core.DTOs.ContactDTOs
{
    public class ContactUpdateDto
    {
        public Guid ContactId { get; init; }
        public string MapLocation { get; init; } = null!;
        public string Email { get; init; } = null!;
        public string Address { get; init; } = null!;
        public string Phone { get; init; } = null!;
        public string OpenHours { get; init; } = null!;
    }
}
