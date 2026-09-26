using System;

namespace Yummy.Entity
{
    public class Feature : BaseEntity
    {
        public Guid FeatureId { get; set; }
        public string Title { get; set; } = null!;
        public string SubTitle { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string VideoUrl { get; set; } = null!;
        public string ImageUrl { get; set; } = null!;
    }
}
