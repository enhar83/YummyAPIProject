using System;
using System.Collections.Generic;

namespace Yummy.Entity
{
    public class DiningTable
    {
        public Guid DiningTableId { get; set; }
        public string TableNo { get; set; } = null!;
        public int Capacity { get; set; }
        public bool IsActive { get; set; } = true;
        
        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    }
}
