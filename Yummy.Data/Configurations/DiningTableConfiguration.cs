using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Yummy.Entity;

namespace Yummy.Data.Configurations
{
    public class DiningTableConfiguration : IEntityTypeConfiguration<DiningTable>
    {
        public void Configure(EntityTypeBuilder<DiningTable> builder)
        {
            // validator ile aynı sınır (50). nvarchar(max) kolonlara index eklenemediği için uzunluk sınırı unique index için de gereklidir.
            builder.Property(t => t.TableNo)
                .HasMaxLength(50);

            // masa numarası benzersizdir. manager'daki kontrol kullanıcıya anlaşılır bir mesaj verir;
            // bu index ise eşzamanlı iki istekte bile aynı numaralı ikinci masanın oluşmasını veritabanı seviyesinde engeller.
            builder.HasIndex(t => t.TableNo)
                .IsUnique();
        }
    }
}
