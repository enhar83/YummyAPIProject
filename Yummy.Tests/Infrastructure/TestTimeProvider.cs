namespace Yummy.Tests.Infrastructure
{
    // testlerde saat sabitlenir ve istenildiğinde ileri alınır; böylece testler çalıştırıldıkları saate bağlı olmaz.
    // saat dilimi uygulamadaki varsayılan ile aynıdır (Europe/Istanbul).
    public class TestTimeProvider : TimeProvider
    {
        private static readonly TimeZoneInfo RestaurantTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");

        private DateTimeOffset _utcNow;

        public TestTimeProvider(DateTime localNow) => SetLocalNow(localNow);

        public override TimeZoneInfo LocalTimeZone => RestaurantTimeZone;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void SetLocalNow(DateTime localNow) =>
            _utcNow = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localNow, DateTimeKind.Unspecified), RestaurantTimeZone));

        public void Advance(TimeSpan duration) => _utcNow = _utcNow.Add(duration);
    }
}
