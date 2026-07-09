using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yummy.Core.DTOs.ReservationDTOs
{
    public class CheckAvailabilityResponseDto
    {
        public DateTime ReservationDate { get; set; }
        public bool IsFullyBooked { get; set; } 
        public List<string> AvailableTimeSlots { get; set; } = new List<string>();
    }
}
