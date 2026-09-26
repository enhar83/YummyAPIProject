using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Yummy.Entity;

namespace Yummy.Data.Configurations
{
    public class TestimonialConfiguration : IEntityTypeConfiguration<Testimonial>
    {
        public void Configure(EntityTypeBuilder<Testimonial> builder)
        {
            // Rating 1-5 arasında olmalıdır. CheckConstraint veritabanı seviyesinde de kısıtlar.
            builder.ToTable(t => t.HasCheckConstraint("CK_Testimonial_Rating", "[Rating] >= 1 AND [Rating] <= 5"));

            builder.HasOne(t => t.AppUser)
                .WithMany(u => u.Testimonials)
                .HasForeignKey(t => t.AppUserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
