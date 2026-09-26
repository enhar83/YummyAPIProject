using System;

namespace Yummy.Business.Time
{
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

// o saat dilimine göre çalışan saattır. DI ile manager ve validatorlara verilir.
