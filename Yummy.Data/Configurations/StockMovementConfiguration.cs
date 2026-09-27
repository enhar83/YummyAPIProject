using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Yummy.Entity;

namespace Yummy.Data.Configurations
{
    public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
    {
        public void Configure(EntityTypeBuilder<StockMovement> builder)
        {
            builder.Property(m => m.QuantityChange)
                .HasColumnType("decimal(18,3)");

            builder.Property(m => m.QuantityAfter)
                .HasColumnType("decimal(18,3)");

            // validator ile aynı sınır.
            builder.Property(m => m.Note)
                .HasMaxLength(250);

            // Ingredient → Restrict: malzemeler soft delete ile silindiği için hareket geçmişi korunur.
            builder.HasOne(m => m.Ingredient)
                .WithMany(i => i.StockMovements)
                .HasForeignKey(m => m.IngredientId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(m => m.PerformedByUser)
                .WithMany()
                .HasForeignKey(m => m.PerformedByUserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // hareket geçmişi malzeme bazında en yeniden eskiye listelenir.
            builder.HasIndex(m => new { m.IngredientId, m.CreatedDate });
        }
    }
}
