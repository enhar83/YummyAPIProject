namespace Yummy.Core.Constants
{
    // sistem tarafından kullanılan sabit rol isimleri.
    public static class RoleNames
    {
        // admin controller'ları [Authorize(Roles = RoleNames.Admin)] ile korunur. Bu rol silinir, adı değişir veya pasife alınırsa kimse admin paneline erişemez;
        // bu nedenle AppRoleManager ve AppUserManager içerisinde koruma altındadır.
        public const string Admin = "Admin";
        public const string Employee = "Employee";

        // kayıt olan her kullanıcıya otomatik atanır. Kayıt akışı bu role bağlı olduğu için AppRoleManager içerisinde koruma altındadır.
        public const string Customer = "Customer";

        // silinemeyen, adı değiştirilemeyen ve pasife alınamayan sistem rolleri.
        public static readonly IReadOnlyList<string> SystemRoles = new[] { Admin, Customer };
    }
}
