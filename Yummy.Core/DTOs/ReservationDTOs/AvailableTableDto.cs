using System;

namespace Yummy.Core.DTOs.ReservationDTOs
{
    public class AvailableTableDto
    {
        public Guid DiningTableId { get; set; }
        public string TableNo { get; set; }
        public int Capacity { get; set; }
    }
}
