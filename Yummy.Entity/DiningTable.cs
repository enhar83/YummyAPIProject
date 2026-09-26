using System;
using System.Collections.Generic;

namespace Yummy.Entity
{
    public class DiningTable : BaseEntity
    {
        public Guid DiningTableId { get; set; }
        public string TableNo { get; set; } = null!;
        public int Capacity { get; set; }
        public bool IsActive { get; set; } = true;
        public string? Location { get; set; }

        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>(); // bir masa birden fazla rezervasyon için kullanılabilir.
    }
}
