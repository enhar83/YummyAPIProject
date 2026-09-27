using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Yummy.Entity;

namespace Yummy.Data.Configurations
{
    public class ChefConfiguration : IEntityTypeConfiguration<Chef>
    {
        public void Configure(EntityTypeBuilder<Chef> builder)
        {
            // AppUser → SetNull: kullanıcı hesabı silinirse şef profili vitrinde kalır, sadece hesap bağlantısı kopar.
            builder.HasOne(c => c.AppUser)
                .WithMany()
                .HasForeignKey(c => c.AppUserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // bir kullanıcı en fazla bir şef profiline bağlanabilir. manager'daki kontrol anlaşılır bir mesaj verir;
            // bu index ise eşzamanlı iki istekte bile aynı kullanıcının iki profile bağlanmasını veritabanı seviyesinde engeller.
            // hesabı olmayan (AppUserId boş) şefler bu kısıttan etkilenmez.
            builder.HasIndex(c => c.AppUserId)
                .IsUnique()
                .HasFilter("[AppUserId] IS NOT NULL");
        }
    }
}
