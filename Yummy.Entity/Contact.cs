using System;

namespace Yummy.Entity
{
    public class Contact : BaseEntity
    {
        public Guid ContactId { get; set; }
        public string MapLocation { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Address { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string OpenHours { get; set; } = null!;
    }
}
