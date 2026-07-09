using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yummy.Core.DTOs.ReservationDTOs
{
    public class ReservationUpdateDto
    {
        public Guid ReservationId { get; set; }
        public DateTime ReservationDate { get; set; }
        public string ReservationTime { get; set; } = null!;
        public int NumberOfGuests { get; set; }
        public string? Message { get; set; }
    }
}
