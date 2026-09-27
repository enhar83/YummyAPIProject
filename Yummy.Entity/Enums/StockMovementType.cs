namespace Yummy.Entity.Enums
{
    // stoğu değiştiren her işlem bir hareket kaydı bırakır; stoğun neden ve kim tarafından değiştiği geriye dönük izlenebilir.
    public enum StockMovementType
    {
        StockIn = 1,          // çalışanın elle yaptığı stok girişi (örn. rutin alım)
        Waste = 2,            // fire / bozulma nedeniyle stoktan düşüm
        CountCorrection = 3   // sayım sonucu stoğun gerçek miktara eşitlenmesi (artı veya eksi)
    }
}
