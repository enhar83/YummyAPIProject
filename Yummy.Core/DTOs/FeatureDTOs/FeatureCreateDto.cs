using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Yummy.Core.DTOs.FeatureDTOs
{
    public class FeatureCreateDto
    {
        public string Title { get; init; } = null!;
        public string SubTitle { get; init; } = null!;
        public string Description { get; init; } = null!;
        public string VideoUrl { get; init; } = null!;
        public IFormFile? Image { get; init; }
    }
}
