using System;

namespace Yummy.Core.DTOs.ReservationDTOs
{
    public class TableStatusForMapDto
    {
        public Guid DiningTableId { get; set; }
        public string TableNo { get; set; } = null!;
        public string? Location { get; set; }
        public int Capacity { get; set; }
        public bool IsAvailable { get; set; }
    }
}
