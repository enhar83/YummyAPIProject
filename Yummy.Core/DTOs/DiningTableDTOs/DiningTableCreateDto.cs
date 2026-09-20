using System;

namespace Yummy.Core.DTOs.DiningTableDTOs
{
    public class DiningTableCreateDto
    {
        public string TableNo { get; set; }
        public int Capacity { get; set; }
        public bool IsActive { get; set; }
    }
}
