using System;
using Yummy.Entity.Enums;

namespace Yummy.Entity
{
    public class Reservation : BaseEntity
    {
        public Guid ReservationId { get; set; }
        // name ve surname'in AppUser'dan çekilmemesinin nedeni, rezervasyonun başka birisi adına yapılabilmesidir.
        public string Name { get; set; } = null!;
        public string Surname { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public DateTime ReservationDate { get; set; }
        public string ReservationTime { get; set; } = null!;
        public string ReservationEndTime { get; set; } = null!;
        public int NumberOfGuests { get; set; }
        public string Message { get; set; } = null!;
        public ReservationStatus ReservationStatus { get; set; }
        public Guid AppUserId { get; set; }
        public AppUser AppUser { get; set; } = null!;
        public Guid DiningTableId { get; set; }
        public DiningTable DiningTable { get; set; } = null!;
    }
}
