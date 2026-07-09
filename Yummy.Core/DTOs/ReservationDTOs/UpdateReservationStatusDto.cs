using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yummy.Entity.Enums;

namespace Yummy.Core.DTOs.ReservationDTOs
{
    public class UpdateReservationStatusDto
    {
        public Guid ReservationId { get; set; }
        public ReservationStatus ReservationStatus { get; set; }
    }
}
