using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Yummy.Entity;

namespace Yummy.Data.Configurations
{
    public class IngredientRequestConfiguration : IEntityTypeConfiguration<IngredientRequest>
    {
        public void Configure(EntityTypeBuilder<IngredientRequest> builder)
        {
            // not uzunlukları validator'lar ile aynıdır. ChefName sınırsızdır, çünkü Chef.Name/Surname alanlarında uzunluk sınırı yoktur.
            builder.Property(r => r.ChefNote).HasMaxLength(500);
            builder.Property(r => r.ResponseNote).HasMaxLength(500);

            // Chef → Restrict: şefler soft delete ile silinir; talep geçmişi korunur.
            builder.HasOne(r => r.Chef)
                .WithMany()
                .HasForeignKey(r => r.ChefId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.HandledByUser)
                .WithMany()
                .HasForeignKey(r => r.HandledByUserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // çalışan listesi duruma göre filtrelenir, şef listesi şefe göre; ikisi de en yeniden eskiye sıralanır.
            builder.HasIndex(r => new { r.Status, r.CreatedDate });
            builder.HasIndex(r => new { r.ChefId, r.CreatedDate });
        }
    }

    public class IngredientRequestItemConfiguration : IEntityTypeConfiguration<IngredientRequestItem>
    {
        public void Configure(EntityTypeBuilder<IngredientRequestItem> builder)
        {
            builder.Property(i => i.IngredientName).HasMaxLength(100);
            builder.Property(i => i.RequestedQuantity).HasColumnType("decimal(18,3)");
            builder.Property(i => i.SuppliedQuantity).HasColumnType("decimal(18,3)");

            // satırlar talebin parçasıdır; talep silinirse (fiziksel silme olmaz, yine de) satırlar da silinir.
            builder.HasOne(i => i.IngredientRequest)
                .WithMany(r => r.Items)
                .HasForeignKey(i => i.IngredientRequestId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            // Ingredient → Restrict: malzemeler soft delete ile silinir; talep satırı ad/birim bilgisini kendisi tutar.
            builder.HasOne(i => i.Ingredient)
                .WithMany()
                .HasForeignKey(i => i.IngredientId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
