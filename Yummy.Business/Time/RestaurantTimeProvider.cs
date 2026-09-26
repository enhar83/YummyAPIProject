using System;

namespace Yummy.Business.Time
{
    // sistem saatini kullanır fakat yerel saat dilimi olarak sunucunun değil restoranın saat dilimini döner.
    // böylece TimeProvider.GetLocalNow() sunucu nerede çalışırsa çalışsın restoranın yerel saatini verir.
    public class RestaurantTimeProvider : TimeProvider
    {
        private readonly TimeZoneInfo _timeZone;

        public RestaurantTimeProvider(TimeZoneInfo timeZone)
        {
            _timeZone = timeZone;
        }

        public override TimeZoneInfo LocalTimeZone => _timeZone;
    }
}
