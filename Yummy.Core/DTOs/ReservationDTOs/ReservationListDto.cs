using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yummy.Core.DTOs.ReservationDTOs
{
    public class ReservationListDto
    {
        public Guid ReservationId { get; set; }
        public string Name { get; set; } = null!;
        public string Surname { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public DateTime ReservationDate { get; set; }
        public string ReservationTime { get; set; } = null!;
        public int NumberOfGuests { get; set; }
        public string Message { get; set; } = null!;
        public string ReservationStatus { get; set; } = null!;
    }
}
