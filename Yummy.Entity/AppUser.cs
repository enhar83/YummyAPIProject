using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace Yummy.Entity
{
    public class AppUser:IdentityUser<Guid>
    {
        public string Name { get; set; } = null!;
        public string Surname { get; set; } = null!;
        public string? ImageUrl { get; set; }
        public bool IsDeleted { get; set; }
        public string? ActivationCode { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public ICollection<Message> Messages { get; set; } = new HashSet<Message>();
        public ICollection<Reservation> Reservations { get; set; } = new HashSet<Reservation>();
        public ICollection<Testimonial> Testimonials { get; set; } = new HashSet<Testimonial>();
    }
}
