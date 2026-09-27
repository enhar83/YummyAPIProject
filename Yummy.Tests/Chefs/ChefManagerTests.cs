using Microsoft.EntityFrameworkCore;
using Yummy.Business.Managers;
using Yummy.Business.Seeding;
using Yummy.Core.Constants;
using Yummy.Core.DTOs.AppUserDTOs;
using Yummy.Core.DTOs.ChefDTOs;
using Yummy.Core.Exceptions;
using Yummy.Core.Settings;
using Yummy.Entity;
using Yummy.Tests.Infrastructure;

namespace Yummy.Tests.Chefs
{
    // sistem rolleri, şef profili ↔ kullanıcı hesabı bağlantısı ve şef paneli profili.
    public class ChefManagerTests : SqliteTestBase
    {
        private static readonly Guid ChefXId = Guid.NewGuid();
        private static readonly Guid ChefYId = Guid.NewGuid();

        public ChefManagerTests()
        {
            using var db = CreateDbContext();
            SystemRoleSeeder.SeedAsync(CreateRoleManager(db)).GetAwaiter().GetResult();
            db.Chefs.AddRange(
                new Chef { ChefId = ChefXId, Name = "Ayşe", Surname = "Yılmaz", Title = "Baş Şef", Description = "", ImageUrl = "" },
                new Chef { ChefId = ChefYId, Name = "Mehmet", Surname = "Demir", Title = "Pasta Şefi", Description = "", ImageUrl = "" });
            db.SaveChanges();
        }

        private async Task LinkAsync(Guid chefId, Guid userId)
        {
            await using var db = CreateDbContext();
            await CreateChefManager(db).LinkUserAsync(chefId, new ChefLinkUserDto { UserId = userId });
        }

        private async Task<bool> IsChefAsync(Guid userId)
        {
            await using var db = CreateDbContext();
            var userManager = CreateUserManager(db);
            return await userManager.IsInRoleAsync((await userManager.FindByIdAsync(userId.ToString()))!, RoleNames.Chef);
        }

        private async Task<Chef> GetChefAsync(Guid chefId)
        {
            await using var db = CreateDbContext();
            return await db.Chefs.IgnoreQueryFilters().SingleAsync(c => c.ChefId == chefId);
        }

        private async Task<string?> GetSecurityStampAsync(Guid userId)
        {
            await using var db = CreateDbContext();
            return (await db.Users.SingleAsync(u => u.Id == userId)).SecurityStamp;
        }

        // ---------- sistem rolleri ----------

        [Fact]
        public async Task SystemRoleSeeder_CreatesAllSystemRoles_AndIsIdempotent()
        {
            await using (var db = CreateDbContext())
                await SystemRoleSeeder.SeedAsync(CreateRoleManager(db)); // constructor'da bir kez çalıştı; ikinci çalıştırma hata vermemeli ve kopya oluşturmamalı.

            await using var check = CreateDbContext();
            var roleNames = await check.Roles.Select(r => r.Name).ToListAsync();
            Assert.Equal(RoleNames.SystemRoles.OrderBy(n => n), roleNames.OrderBy(n => n));
        }

        [Fact]
        public async Task SystemRoleSeeder_ReactivatesSoftDeletedSystemRole()
        {
            await using (var db = CreateDbContext())
            {
                (await db.Roles.SingleAsync(r => r.Name == RoleNames.Employee)).IsDeleted = true;
                await db.SaveChangesAsync();
            }

            await using (var db = CreateDbContext())
                await SystemRoleSeeder.SeedAsync(CreateRoleManager(db));

            await using var check = CreateDbContext();
            Assert.Single(await check.Roles.Where(r => r.Name == RoleNames.Employee).ToListAsync());
        }

        [Theory]
        [InlineData(RoleNames.Chef)]
        [InlineData(RoleNames.Employee)]
        public async Task ChefAndEmployeeRoles_AreProtected(string roleName)
        {
            await using var db = CreateDbContext();
            var role = await db.Roles.SingleAsync(r => r.Name == roleName);
            var roleManager = new AppRoleManager(CreateRoleManager(db), Mapper, CreateUserManager(db));

            var ex = await Assert.ThrowsAsync<LogicException>(() => roleManager.DeleteRoleAsync(role.Id));
            Assert.Equal("ProtectedRole", ex.PropertyName);
        }

        // ---------- bağlama ----------

        [Fact]
        public async Task LinkUser_LinksProfile_AssignsChefRole_AndRevokesSessions()
        {
            var stampBefore = await GetSecurityStampAsync(UserA);

            await LinkAsync(ChefXId, UserA);

            Assert.Equal(UserA, (await GetChefAsync(ChefXId)).AppUserId);
            Assert.True(await IsChefAsync(UserA));
            Assert.NotEqual(stampBefore, await GetSecurityStampAsync(UserA)); // eski token'lar geçersiz; yeni rol ile tekrar giriş yapılmalı.
        }

        [Fact]
        public async Task LinkUser_WhenUserAlreadyLinkedToAnotherChef_Throws()
        {
            await LinkAsync(ChefXId, UserA);

            var ex = await Assert.ThrowsAsync<LogicException>(() => LinkAsync(ChefYId, UserA));
            Assert.Equal("UserAlreadyLinked", ex.PropertyName);
            Assert.Null((await GetChefAsync(ChefYId)).AppUserId);
        }

        [Fact]
        public async Task LinkUser_WhenChefAlreadyLinkedToAnotherUser_Throws()
        {
            await LinkAsync(ChefXId, UserA);

            var ex = await Assert.ThrowsAsync<LogicException>(() => LinkAsync(ChefXId, UserB));
            Assert.Equal("ChefAlreadyLinked", ex.PropertyName);
            Assert.Equal(UserA, (await GetChefAsync(ChefXId)).AppUserId);
            Assert.False(await IsChefAsync(UserB));
        }

        [Fact]
        public async Task LinkUser_SameUserAgain_ThrowsAlreadyLinked()
        {
            await LinkAsync(ChefXId, UserA);

            var ex = await Assert.ThrowsAsync<LogicException>(() => LinkAsync(ChefXId, UserA));
            Assert.Equal("AlreadyLinked", ex.PropertyName);
        }

        [Fact]
        public async Task LinkUser_UnknownUser_Throws_AndProfileStaysUnlinked()
        {
            var ex = await Assert.ThrowsAsync<LogicException>(() => LinkAsync(ChefXId, Guid.NewGuid()));
            Assert.Equal("UserNotFound", ex.PropertyName);
            Assert.Null((await GetChefAsync(ChefXId)).AppUserId);
        }

        [Fact]
        public async Task LinkUser_UserAlreadyInChefRole_KeepsSession()
        {
            // rolü zaten olan kullanıcının token'ı değişmeyeceği için oturumu sonlandırılmaz.
            await using (var db = CreateDbContext())
            {
                var userManager = CreateUserManager(db);
                await userManager.AddToRoleAsync((await userManager.FindByIdAsync(UserA.ToString()))!, RoleNames.Chef);
            }
            var stampBefore = await GetSecurityStampAsync(UserA);

            await LinkAsync(ChefXId, UserA);

            Assert.Equal(stampBefore, await GetSecurityStampAsync(UserA));
        }

        [Fact]
        public async Task Database_RejectsSameUserOnTwoChefs_ButAllowsManyUnlinkedChefs()
        {
            await using var db = CreateDbContext();
            db.Chefs.Add(new Chef { Name = "Hesapsız", Surname = "Şef", Title = "", Description = "", ImageUrl = "" }); // üçüncü AppUserId = null kaydı
            await db.SaveChangesAsync();

            (await db.Chefs.SingleAsync(c => c.ChefId == ChefXId)).AppUserId = UserA;
            (await db.Chefs.SingleAsync(c => c.ChefId == ChefYId)).AppUserId = UserA;
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }

        // ---------- bağlantıyı kaldırma ve silme ----------

        [Fact]
        public async Task UnlinkUser_RemovesLinkAndChefRole()
        {
            await LinkAsync(ChefXId, UserA);
            var stampAfterLink = await GetSecurityStampAsync(UserA);

            await using (var db = CreateDbContext())
                await CreateChefManager(db).UnlinkUserAsync(ChefXId);

            Assert.Null((await GetChefAsync(ChefXId)).AppUserId);
            Assert.False(await IsChefAsync(UserA));
            Assert.NotEqual(stampAfterLink, await GetSecurityStampAsync(UserA));
        }

        [Fact]
        public async Task UnlinkUser_WhenNotLinked_Throws()
        {
            await using var db = CreateDbContext();
            var ex = await Assert.ThrowsAsync<LogicException>(() => CreateChefManager(db).UnlinkUserAsync(ChefXId));
            Assert.Equal("NotLinked", ex.PropertyName);
        }

        [Fact]
        public async Task DeleteLinkedChef_RemovesChefRole_AndUserCanBeLinkedToAnotherChef()
        {
            await LinkAsync(ChefXId, UserA);

            await using (var db = CreateDbContext())
                await CreateChefManager(db).DeleteAsync(ChefXId);

            var deleted = await GetChefAsync(ChefXId);
            Assert.True(deleted.IsDeleted);
            Assert.Null(deleted.AppUserId); // soft delete edilen kayıt unique index'i işgal etmez.
            Assert.False(await IsChefAsync(UserA));

            await LinkAsync(ChefYId, UserA);
            Assert.True(await IsChefAsync(UserA));
        }

        [Fact]
        public async Task UpdateChef_KeepsUserLink()
        {
            await LinkAsync(ChefXId, UserA);

            await using (var db = CreateDbContext())
                await CreateChefManager(db).UpdateAsync(new ChefUpdateDto { ChefId = ChefXId, Name = "Ayşe", Surname = "Yılmaz", Title = "Yönetici Şef", Description = "Yeni açıklama" });

            var chef = await GetChefAsync(ChefXId);
            Assert.Equal("Yönetici Şef", chef.Title);
            Assert.Equal(UserA, chef.AppUserId);
        }

        // ---------- şef paneli ve listeleme ----------

        [Fact]
        public async Task GetMyProfile_ReturnsLinkedChefProfile()
        {
            await LinkAsync(ChefXId, UserA);

            await using var db = CreateDbContext();
            var profile = await CreateChefManager(db).GetMyProfileAsync(UserA.ToString());

            Assert.Equal(ChefXId, profile.ChefId);
            Assert.Equal("Baş Şef", profile.Title);
        }

        [Fact]
        public async Task GetMyProfile_WhenUserNotLinked_Throws()
        {
            await using var db = CreateDbContext();
            var ex = await Assert.ThrowsAsync<LogicException>(() => CreateChefManager(db).GetMyProfileAsync(UserB.ToString()));
            Assert.Equal("ChefProfileNotFound", ex.PropertyName);
        }

        [Fact]
        public async Task GetAll_ShowsLinkedUserEmail()
        {
            await LinkAsync(ChefXId, UserA);

            await using var db = CreateDbContext();
            var chefs = (await CreateChefManager(db).GetAllAsync()).ToList();

            Assert.Equal("a@test.com", chefs.Single(c => c.ChefId == ChefXId).LinkedUserEmail);
            Assert.Null(chefs.Single(c => c.ChefId == ChefYId).LinkedUserEmail);
        }

        // ---------- giriş cevabı ----------

        [Fact]
        public async Task Login_ReturnsUserRoles_ForPanelRedirect()
        {
            var userId = Guid.NewGuid();
            await using (var db = CreateDbContext())
            {
                var userManager = CreateUserManager(db);
                var user = new AppUser { Id = userId, UserName = "sef", Email = "sef@test.com", Name = "Şef", Surname = "Kullanıcı", EmailConfirmed = true };
                Assert.True((await userManager.CreateAsync(user, "123456")).Succeeded);
            }
            await LinkAsync(ChefXId, userId);

            await using var ctx = CreateDbContext();
            var jwt = new JwtManager(Microsoft.Extensions.Options.Options.Create(new JwtSettings { Issuer = "test", Audience = "test", AccessTokenExpiration = 5, SecurityKey = new string('k', 32) }));
            var appUserManager = new AppUserManager(CreateUserManager(ctx), CreateRoleManager(ctx), Mapper, Email, jwt, WebHostEnvironment);

            var response = await appUserManager.LoginAsync(new AppUserLoginDto { Email = "sef@test.com", Password = "123456" });

            Assert.Equal(new[] { RoleNames.Chef }, response.Roles);
        }
    }
}
