using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yummy.Core.DTOs.FeatureDTOs
{
    public class FeatureResponseDto
    {
        public Guid FeatureId { get; init; }
        public string Title { get; init; } = null!;
        public string SubTitle { get; init; } = null!;
        public string Description { get; init; } = null!;
        public string VideoUrl { get; init; } = null!;
        public string ImageUrl { get; init; } = null!;
    }
}
