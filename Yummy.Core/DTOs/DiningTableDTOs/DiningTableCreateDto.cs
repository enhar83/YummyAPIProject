using System;

namespace Yummy.Core.DTOs.DiningTableDTOs
{
    public class DiningTableCreateDto
    {
        public string TableNo { get; set; } = null!;
        public int Capacity { get; set; }
        public bool IsActive { get; set; } = true; // istekte gönderilmezse masa aktif olarak oluşturulur.
        public string? Location { get; set; }
    }
}
