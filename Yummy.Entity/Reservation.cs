using System.Threading;
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yummy.Entity.Enums;

namespace Yummy.Entity
{
    public class Reservation
    {
        public Guid ReservationId { get; set; }
        public string Name { get; set; } = null!;
        public string Surname { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public DateTime ReservationDate { get; set; }
        public string ReservationTime { get; set; } = null!;
        public int NumberOfGuests { get; set; }
        public string Message { get; set; } = null!;
        public ReservationStatus ReservationStatus { get; set; }
        public Guid AppUserId { get; set; }
        public AppUser AppUser { get; set; } = null!;
    }
}
