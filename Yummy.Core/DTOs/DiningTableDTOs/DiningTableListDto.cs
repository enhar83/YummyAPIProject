using System;

namespace Yummy.Core.DTOs.DiningTableDTOs
{
    public class DiningTableListDto
    {
        public Guid DiningTableId { get; set; }
        public string TableNo { get; set; } = null!;
        public int Capacity { get; set; }
        public bool IsActive { get; set; }
        public string? Location { get; set; }
    }
}
