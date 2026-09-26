using System.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yummy.Core.DTOs.ReservationDTOs
{
    // sadece admin endpoint'lerinde (liste, detay, günün rezervasyonları) kullanılır.
    public class ReservationListDto
    {
        public Guid ReservationId { get; set; }
        public string Name { get; set; } = null!;
        public string Surname { get; set; } = null!;
        public string Email { get; set; } = null!; // admin'in müşteriye ulaşabilmesi için. entity'deki Email alanından AutoMapper ile isim eşleşmesiyle doldurulur.
        public string Phone { get; set; } = null!;
        public DateTime ReservationDate { get; set; }
        public string ReservationTime { get; set; } = null!;
        public string ReservationEndTime { get; set; } = null!;
        public Guid DiningTableId { get; set; }
        public string TableNo { get; set; } = null!;
        public string? Location { get; set; }
        public int NumberOfGuests { get; set; }
        public string Message { get; set; } = null!;
        public string ReservationStatus { get; set; } = null!;
    }
}
