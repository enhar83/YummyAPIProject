namespace Yummy.Entity.Enums
{
    // çalışanın elle yapabileceği stok hareketleri. StockMovementType'ın alt kümesidir (değerler aynıdır);
    // sistemin kendi ürettiği hareketler (örn. talep tedariki) elle girilemesin diye ayrı tutulur.
    public enum StockAdjustmentType
    {
        StockIn = 1,
        Waste = 2,
        CountCorrection = 3
    }
}
