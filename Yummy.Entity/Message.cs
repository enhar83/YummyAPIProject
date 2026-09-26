using System;

namespace Yummy.Entity
{
    public class Message : BaseEntity
    {
        public Guid MessageId { get; set; }
        public string Name { get; set; } = null!;
        public string Surname { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Subject { get; set; } = null!;
        public string MessageDetails { get; set; } = null!;
        public DateTime SendDate { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; }

        // nullable: anonim kullanıcıların da mesaj gönderebilmesi için.
        public Guid? AppUserId { get; set; }
        public AppUser? AppUser { get; set; }
    }
}
