using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yummy.Entity
{
    public class Message
    {
        public Guid MessageId { get; set; }
        public string Name { get; set; } = null!;
        public string Surname { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Subject { get; set; } = null!;
        public string MessageDetails { get; set; } = null!;
        public DateTime SendDate { get; set; } = DateTime.Now;
        public bool IsRead { get; set; }
    }
}
