using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Yummy.Core.DTOs.ProductDTOs
{
    public record ProductUpdateDto
    {
        public Guid ProductId { get; init; }
        public string ProductName { get; init; } = null!;
        public string ProductDescription { get; init; } = null!;
        public decimal Price { get; init; }
        public IFormFile? Image { get; init; }
        public Guid CategoryId { get; init; }
    }
}