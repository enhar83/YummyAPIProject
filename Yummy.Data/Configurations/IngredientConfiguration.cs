using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Yummy.Entity;

namespace Yummy.Data.Configurations
{
    public class IngredientConfiguration : IEntityTypeConfiguration<Ingredient>
    {
        public void Configure(EntityTypeBuilder<Ingredient> builder)
        {
            // validator ile aynı sınır. nvarchar(max) kolonlara index eklenemediği için uzunluk sınırı unique index için de gereklidir.
            builder.Property(i => i.Name)
                .HasMaxLength(100);

            // 3 ondalık: 0,250 kg veya 1,5 litre gibi miktarlar tutulabilir.
            builder.Property(i => i.StockQuantity)
                .HasColumnType("decimal(18,3)");

            // silinmemiş malzemeler arasında ad benzersizdir; silinen bir malzemenin adı yeni bir kartta tekrar kullanılabilir.
            builder.HasIndex(i => i.Name)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
        }
    }
}
