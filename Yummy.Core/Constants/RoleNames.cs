namespace Yummy.Core.Constants
{
    // sistem tarafından kullanılan sabit rol isimleri.
    public static class RoleNames
    {
        // admin controller'ları [Authorize(Roles = "Admin")] ile korunur. Bu rol silinir, adı değişir veya pasife alınırsa kimse admin paneline erişemez;
        // bu nedenle AppRoleManager ve AppUserManager içerisinde koruma altındadır.
        public const string Admin = "Admin";
    }
}
