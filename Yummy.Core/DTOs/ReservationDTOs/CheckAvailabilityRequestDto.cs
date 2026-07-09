using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yummy.Core.DTOs.ReservationDTOs
{
    public class CheckAvailabilityRequestDto
    {
        public DateTime ReservationDate { get; set; }
        public int NumberOfGuests { get; set; }
    }
}
