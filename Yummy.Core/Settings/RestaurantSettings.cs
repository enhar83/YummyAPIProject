namespace Yummy.Core.Settings
{
    public class RestaurantSettings
    {
        // restoranın bulunduğu saat dilimi (IANA formatı). rezervasyon saatleri bu saat dilimine göre değerlendirilir.
        public string TimeZoneId { get; set; } = "Europe/Istanbul";
    }
}
