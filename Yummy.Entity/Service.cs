using System;

namespace Yummy.Entity
{
    public class Service : BaseEntity
    {
        public Guid ServiceId { get; set; }
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string IconUrl { get; set; } = null!;
    }
}
