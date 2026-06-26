using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yummy.Core.DTOs.CategoryDTOs
{
    public record CategoryUpdateDto
    {
        public Guid CategoryId { get; init; }
        public string CategoryName { get; init; } = null!;
    }
}