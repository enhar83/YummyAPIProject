namespace Yummy.Entity.Enums
{
    // malzemenin stokta hangi birimle tutulduğu. stok hareketi olan malzemenin birimi değiştirilemez (5 kg'ın 5 gram'a dönüşmemesi için).
    public enum IngredientUnit
    {
        Gram = 1,
        Kilogram = 2,
        Milliliter = 3,
        Liter = 4,
        Piece = 5
    }
}
