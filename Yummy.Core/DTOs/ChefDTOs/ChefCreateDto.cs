using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Yummy.Core.DTOs.ChefDTOs
{
    public record ChefCreateDto
    {
        public string Name { get; init; } = null!;
        public string Surname { get; init; } = null!;
        public string Title { get; init; } = null!;
        public string Description { get; init; } = null!;
        public IFormFile? Image { get; init; }
    }
}