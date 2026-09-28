namespace Yummy.Core.DTOs.IngredientRequestDTOs
{
    public record IngredientRequestRejectDto
    {
        /// <summary>
        /// Red gerekçesi (zorunlu, şefe iletilir). En fazla 500 karakter.
        /// </summary>
        public string Note { get; init; } = null!;
    }
}
