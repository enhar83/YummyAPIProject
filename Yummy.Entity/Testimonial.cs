using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yummy.Entity
{
    public class Testimonial
    {
        public Guid TestimonialId { get; set; }
        public string Title { get; set; } = null!;
        public string Comment { get; set; } = null!;
        public byte Rating { get; set; } 
        public bool IsApproved { get; set; } = false; 
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public Guid AppUserId { get; set; } 
        public AppUser AppUser { get; set; } = null!;
    }
}
