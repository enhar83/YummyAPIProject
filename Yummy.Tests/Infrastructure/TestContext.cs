using AutoMapper;
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

        protected readonly FakeEmailService Email = new();

        static TestContext()
        {
            // manager'lar e-posta şablonlarını Directory.GetCurrentDirectory()/Templates altından okur.
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);
        }

        protected abstract DbContextOptions<YummyDbContext> Options { get; }

        protected YummyDbContext CreateDbContext() => new(Options);

        protected ReservationManager CreateReservationManager(YummyDbContext db) =>
            new(new GenericRepository<Reservation>(db), new GenericRepository<DiningTable>(db), new UnitOfWork(db), Mapper, Email);

        protected static DiningTableManager CreateDiningTableManager(YummyDbContext db) =>
            new(new GenericRepository<DiningTable>(db), new GenericRepository<Reservation>(db), new UnitOfWork(db), Mapper);

        protected static void SeedUsers(YummyDbContext db)
        {
            db.Users.AddRange(
                new AppUser { Id = UserA, UserName = "usera", Name = "User", Surname = "A", Email = "a@test.com" },
                new AppUser { Id = UserB, UserName = "userb", Name = "User", Surname = "B", Email = "b@test.com" });
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
