namespace Yummy.WebAPI.RateLimiting
{
    // rate limiting policy isimleri. Program.cs içerisinde tanımlanır, controller'larda [EnableRateLimiting] ile kullanılır.
    public static class RateLimitPolicies
    {
        // login, refresh, verify-email gibi auth endpoint'leri: IP başına dakikada 10 istek.
        public const string Auth = "auth";

        // e-posta gönderen endpoint'ler (register, forgot-password, request-email-change): IP başına 15 dakikada 5 istek.
        public const string EmailSending = "email-sending";

        // rezervasyon oluşturma/güncelleme/iptal (her biri e-posta gönderir): kullanıcı başına 15 dakikada 10 istek.
        public const string Reservation = "reservation";
    }
}
