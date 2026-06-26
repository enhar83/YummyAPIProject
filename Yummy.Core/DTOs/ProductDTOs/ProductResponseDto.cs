using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yummy.Core.DTOs.ProductDTOs
{
    public record ProductResponseDto(
        Guid ProductId,
        string ProductName,
        string ProductDescription,
        decimal Price,
        string ImageUrl,
        Guid CategoryId,
        string CategoryName
    );
}
