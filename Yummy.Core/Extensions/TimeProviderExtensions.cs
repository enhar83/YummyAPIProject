namespace Yummy.Core.Extensions
{
    public static class TimeProviderExtensions
    {
        // restoranın yerel saatine göre şu an. rezervasyon tarih/saatleri restoranın yerel saatiyle tutulduğu için karşılaştırmalarda bu değer kullanılır.
        // DateTime.Now sunucunun saat dilimine bağlıdır; sunucu UTC ise (bulut, docker) tüm kontroller saatlerce kayar.
        public static DateTime GetLocalDateTime(this TimeProvider timeProvider) => timeProvider.GetLocalNow().DateTime;

        // restoranın yerel saatine göre bugünün tarihi.
        public static DateTime GetLocalToday(this TimeProvider timeProvider) => timeProvider.GetLocalNow().Date;
    }
}
