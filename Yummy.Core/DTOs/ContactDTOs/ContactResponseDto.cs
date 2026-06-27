using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yummy.Core.DTOs.ContactDTOs
{
    public class ContactResponseDto
    {
        public Guid ContactId { get; set; }
        public string MapLocation { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Address { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string OpenHours { get; set; } = null!;
    }
}
