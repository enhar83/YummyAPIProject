namespace Yummy.Core.Extensions
{
    public static class TimeProviderExtensions
    {
        public static DateTime GetLocalDateTime(this TimeProvider timeProvider) => timeProvider.GetLocalNow().DateTime;
        public static DateTime GetLocalToday(this TimeProvider timeProvider) => timeProvider.GetLocalNow().Date;
    }
}

// DateTime.Now ile alınmamasının sebebi, .Now sunucu saatini verir. Sunucu farklı bir saat dilimindeyse UTC gerekmektedir. 
// program.cs içerisinde ayarı yapılmıştır.
// saat dilimi koda gömülmemiş olur. restoran başka bir ülkede açılırsa kod açılmadan ayar değişir.

// bu saatten şu an ve bugün bilgisini tek satırda almayı sağlayan kısayol.
