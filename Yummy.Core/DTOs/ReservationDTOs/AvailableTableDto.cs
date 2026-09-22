using System;

namespace Yummy.Core.DTOs.ReservationDTOs
{
    public class AvailableTableDto
    {
        public Guid DiningTableId { get; set; }
        public string TableNo { get; set; } = null!;
        public int Capacity { get; set; }
    }
}
