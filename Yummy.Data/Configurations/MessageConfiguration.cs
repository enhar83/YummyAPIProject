using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Yummy.Entity;

namespace Yummy.Data.Configurations
{
    public class MessageConfiguration : IEntityTypeConfiguration<Message>
    {
        public void Configure(EntityTypeBuilder<Message> builder)
        {
            // AppUserId nullable yapıldığı için anonim kullanıcı mesajlarına izin verilir.
            // Kullanıcı silindiğinde mesajları NULL olarak kalır (SetNull), silinmez.
            builder.HasOne(m => m.AppUser)
                .WithMany(u => u.Messages)
                .HasForeignKey(m => m.AppUserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
