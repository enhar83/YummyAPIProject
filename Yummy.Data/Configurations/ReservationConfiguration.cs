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

            // validator sınırları ile aynı uzunluklar; saatler "HH:mm" formatındadır.
            builder.Property(r => r.Name).HasMaxLength(50);
            builder.Property(r => r.Surname).HasMaxLength(50);
            builder.Property(r => r.Email).HasMaxLength(100);
            builder.Property(r => r.Phone).HasMaxLength(20);
            builder.Property(r => r.Message).HasMaxLength(500);
            builder.Property(r => r.ReservationTime).HasMaxLength(5);
            builder.Property(r => r.ReservationEndTime).HasMaxLength(5);

            // müsaitlik, harita, günlük liste ve arka plan servisi sorgularının tamamı tarihe göre filtreler.
            builder.HasIndex(r => r.ReservationDate);

            // AppUser → Cascade: Kullanıcı silinince rezervasyonları da silinir.
            builder.HasOne(r => r.AppUser)
                .WithMany(u => u.Reservations)
                .HasForeignKey(r => r.AppUserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            // DiningTable → Restrict: masalar silinmez, sadece pasife alınır; geçmiş rezervasyonların masa bilgisi korunur.
            // Restrict ayrıca AppUser cascade'i ile birlikte "multiple cascade paths" hatasını önler.
            builder.HasOne(r => r.DiningTable)
                .WithMany(t => t.Reservations)
                .HasForeignKey(r => r.DiningTableId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
