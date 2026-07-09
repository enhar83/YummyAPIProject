using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yummy.Core.DTOs.TestimonialDTOs
{
    public class TestimonialCreateDto
    {
        public string Title { get; set; } = null!;
        public string Comment { get; set; } = null!;
        public byte Rating { get; set; } 
    }
}
