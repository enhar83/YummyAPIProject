using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yummy.Core.DTOs.ProductDTOs
{
    public record ProductResponseDto
    {
        public Guid ProductId { get; init; }
        public string ProductName { get; init; } = null!;
        public string ProductDescription { get; init; } = null!;
        public decimal Price { get; init; }
        public string? ImageUrl { get; init; }
        public Guid CategoryId { get; init; }
        public string CategoryName { get; init; } = null!;
    }
}