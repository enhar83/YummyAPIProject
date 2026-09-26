using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Yummy.Entity;
using Yummy.Entity.Enums;

namespace Yummy.Data.Configurations
{
    public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
    {
        public void Configure(EntityTypeBuilder<Reservation> builder)
        {
            // ReservationStatus enum'u veritabanında string olarak saklanır.
            // Bu sayede enum sırası değişse dahi veri bütünlüğü korunur.
            builder.Property(r => r.ReservationStatus)
                .HasConversion<string>()
                .HasMaxLength(20);

            // AppUser → Cascade: Kullanıcı silinince rezervasyonları da silinir.
            builder.HasOne(r => r.AppUser)
                .WithMany(u => u.Reservations)
                .HasForeignKey(r => r.AppUserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            // DiningTable → Restrict: Masa silinmek istenirse önce rezervasyonlar temizlenmelidir.
            // Böylece "multiple cascade paths" hatası önlenir ve veri bütünlüğü sağlanır.
            builder.HasOne(r => r.DiningTable)
                .WithMany(t => t.Reservations)
                .HasForeignKey(r => r.DiningTableId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
