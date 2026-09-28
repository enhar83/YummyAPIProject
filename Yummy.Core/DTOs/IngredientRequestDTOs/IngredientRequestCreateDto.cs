namespace Yummy.Core.DTOs.IngredientRequestDTOs
{
    // şefin çalışanlara ilettiği malzeme talebi.
    public record IngredientRequestCreateDto
    {
        /// <summary>
        /// İstenen malzemeler (en az 1, en fazla 30). Her malzeme listede bir kez yer alabilir.
        /// Sadece stok kartı olan malzemeler istenebilir; listede olmayan bir malzeme için nota yazınız, çalışan yeni kart açar.
        /// </summary>
        public List<IngredientRequestItemCreateDto> Items { get; init; } = new();

        /// <summary>
        /// İsteğe bağlı not (örn. "Cuma akşamı menüsü için, organik olursa iyi olur"). En fazla 500 karakter.
        /// </summary>
        public string? Note { get; init; }
    }

    public record IngredientRequestItemCreateDto
    {
        public Guid IngredientId { get; init; }

        /// <summary>
        /// Malzemenin kendi birimiyle istenen miktar (örn. kg ise 2.5 = 2,5 kg). 0'dan büyük, en fazla 3 ondalık basamak.
        /// </summary>
        public decimal Quantity { get; init; }
    }
}
