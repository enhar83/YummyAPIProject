namespace Yummy.Core.DTOs.IngredientRequestDTOs
{
    // çalışanın talebi tedarik etmesi. her satır için gerçekte gelen miktar girilir; gelen miktar stoğa eklenir.
    public record IngredientRequestSupplyDto
    {
        /// <summary>
        /// Talepteki her satır için bir kayıt (satır sayısı ve kimlikleri talep ile birebir aynı olmalıdır).
        /// </summary>
        public List<IngredientRequestSupplyItemDto> Items { get; init; } = new();

        /// <summary>
        /// Şefe iletilecek isteğe bağlı not (örn. "Domates yarın sabah gelecek"). En fazla 500 karakter.
        /// </summary>
        public string? Note { get; init; }
    }

    public record IngredientRequestSupplyItemDto
    {
        public Guid IngredientRequestItemId { get; init; }

        /// <summary>
        /// Gerçekte tedarik edilen miktar. İstenenden az veya fazla olabilir; 0 = bu malzeme bulunamadı. En fazla 3 ondalık basamak.
        /// En az bir satırda 0'dan büyük olmalıdır (hiçbiri bulunamadıysa talep reddedilmelidir).
        /// </summary>
        public decimal SuppliedQuantity { get; init; }
    }
}
