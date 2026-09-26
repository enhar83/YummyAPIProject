using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Yummy.Entity;

namespace Yummy.Data.Context
{
    public class YummyDbContext : IdentityDbContext<AppUser, AppRole, Guid>
    {
        public YummyDbContext(DbContextOptions<YummyDbContext> options) : base(options)
        {
        }

        public DbSet<Category>    Categories   { get; set; }
        public DbSet<Chef>        Chefs        { get; set; }
        public DbSet<Contact>     Contacts     { get; set; }
        public DbSet<Feature>     Features     { get; set; }
        public DbSet<Gallery>     Galleries    { get; set; }
        public DbSet<DiningTable> DiningTables { get; set; }
        public DbSet<Message>     Messages     { get; set; }
        public DbSet<Product>     Products     { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<Service>     Services     { get; set; }
        public DbSet<Testimonial> Testimonials { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder); // Identity tablolarının yapılandırılması için zorunludur.

            #region Identity Tablo Adları
            // ASP.NET Core Identity'nin varsayılan "AspNet" ön ekini kaldırarak daha temiz tablo adları sağlar.
            builder.Entity<AppUser>().ToTable("Users");
            builder.Entity<AppRole>().ToTable("Roles");
            builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
            builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
            builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
            builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
            builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");
            #endregion

            // Configurations/ klasöründeki tüm IEntityTypeConfiguration<T> sınıflarını otomatik olarak uygular.
            // Yeni bir entity eklendiğinde sadece ilgili Configuration sınıfı yazılır; bu metoda dokunulmaz.
            builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}
