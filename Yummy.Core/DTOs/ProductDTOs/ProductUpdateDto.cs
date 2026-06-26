using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Yummy.Core.DTOs.ProductDTOs
{
    public record ProductUpdateDto(
        Guid ProductId,
        string ProductName,
        string ProductDescription,
        decimal Price,
        IFormFile? Image,
        Guid CategoryId
    );
}
