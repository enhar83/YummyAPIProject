using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yummy.Core.DTOs.TestimonialDTOs
{
    public class UsersPastTestimonialsListDto
    {
        public Guid TestimonialId { get; set; }
        public string Title { get; set; } = null!;
        public string Comment { get; set; } = null!;
        public byte Rating { get; set; }
        public bool IsApproved { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
