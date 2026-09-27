using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Yummy.Business.Managers;
using Yummy.Core.DTOs.ReservationDTOs;
using Yummy.Core.Services;
using Yummy.Data;
using Yummy.Data.Context;
using Yummy.Data.Repositories;
using Yummy.Entity;

namespace Yummy.Tests.Infrastructure
{
    // testlerde gerçek manager, repository, UnitOfWork ve AutoMapper profilleri kullanılır; sadece veritabanı ve e-posta servisi değiştirilir.
    public abstract class TestContext
    {
        public static readonly Guid UserA = Guid.NewGuid();
        public static readonly Guid UserB = Guid.NewGuid();

        protected static readonly IMapper Mapper = new MapperConfiguration(
            cfg => cfg.AddMaps(typeof(ReservationManager).Assembly), NullLoggerFactory.Instance).CreateMapper();

        // testler sabit bir anda (restoranın yerel saatiyle) çalışır. gerektiğinde Clock.SetLocalNow/Advance ile değiştirilir.
        protected static readonly DateTime Now = new(2030, 6, 15, 12, 0, 0);
        protected static DateTime Today => Now.Date;

        protected readonly FakeEmailService Email = new();
        protected readonly TestTimeProvider Clock = new(Now);

        protected abstract DbContextOptions<YummyDbContext> Options { get; }

        protected YummyDbContext CreateDbContext() => new(Options);

        protected ReservationManager CreateReservationManager(YummyDbContext db) =>
            new(new GenericRepository<Reservation>(db), new GenericRepository<DiningTable>(db), new UnitOfWork(db), Mapper, Email, NullLogger<ReservationManager>.Instance, Clock);

        protected DiningTableManager CreateDiningTableManager(YummyDbContext db) =>
            new(new GenericRepository<DiningTable>(db), new GenericRepository<Reservation>(db), new UnitOfWork(db), Mapper, Clock);

        protected readonly FakeWebHostEnvironment WebHostEnvironment = new();

        // Identity yöneticileri uygulamadaki gibi aynı DbContext üzerinde çalışır; böylece UnitOfWork transaction'ına dahil olurlar.
        protected static UserManager<AppUser> CreateUserManager(YummyDbContext db) =>
            new(new UserStore<AppUser, AppRole, YummyDbContext, Guid>(db), Microsoft.Extensions.Options.Options.Create(new IdentityOptions()), new PasswordHasher<AppUser>(),
                Array.Empty<IUserValidator<AppUser>>(), Array.Empty<IPasswordValidator<AppUser>>(), new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(), null!, NullLogger<UserManager<AppUser>>.Instance);

        protected static RoleManager<AppRole> CreateRoleManager(YummyDbContext db) =>
            new(new RoleStore<AppRole, YummyDbContext, Guid>(db), Array.Empty<IRoleValidator<AppRole>>(), new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(), NullLogger<RoleManager<AppRole>>.Instance);

        protected ChefManager CreateChefManager(YummyDbContext db) =>
            new(new GenericRepository<Chef>(db), new UnitOfWork(db), Mapper, WebHostEnvironment, CreateUserManager(db));

        protected static void SeedUsers(YummyDbContext db)
        {
            db.Users.AddRange(
                new AppUser { Id = UserA, UserName = "usera", Name = "User", Surname = "A", Email = "a@test.com", SecurityStamp = Guid.NewGuid().ToString("N") },
                new AppUser { Id = UserB, UserName = "userb", Name = "User", Surname = "B", Email = "b@test.com", SecurityStamp = Guid.NewGuid().ToString("N") });
        }

        protected static ReservationCreateDto CreateDto(DateTime date, int guests = 2, string start = "19:00", string end = "21:00", Guid? tableId = null) => new()
        {
            Name = "Test",
            Surname = "Kullanıcı",
            Email = "test@test.com",
            Phone = "5550000000",
            ReservationDate = date,
            ReservationTime = start,
            ReservationEndTime = end,
            NumberOfGuests = guests,
            SelectedTableId = tableId
        };
    }
}
