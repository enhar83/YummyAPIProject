using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Yummy.Business.Managers;
using Yummy.Core.Exceptions;
using Yummy.Data.Context;
using Yummy.Entity;
using Yummy.Tests.Infrastructure;

namespace Yummy.Tests.Reservations
{
    // sp_getapplock SQL Server'a özgü olduğu için bu testler gerçek bir SQL Server (varsayılan: LocalDB) üzerinde,
    // her çalıştırmada oluşturulup sonunda silinen geçici bir veritabanında koşar. SQL Server'a erişilemezse testler atlanır (Skipped).
    // farklı bir sunucu kullanmak için YUMMY_TEST_SQLSERVER ortam değişkenine "Server=...;" kısmı verilebilir.
    public class ReservationConcurrencyTests : TestContext, IAsyncLifetime
    {
        private const int ConcurrentRequests = 10;
        private static readonly DateTime FutureDay = Now.Date.AddDays(3);
        private readonly Guid[] _extraUsers = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToArray();

        private readonly string _connectionString;
        private bool _isAvailable;

        protected override DbContextOptions<YummyDbContext> Options { get; }

        public ReservationConcurrencyTests()
        {
            var server = Environment.GetEnvironmentVariable("YUMMY_TEST_SQLSERVER") ?? @"Server=(localdb)\MSSQLLocalDB;";
            _connectionString = $"{server.TrimEnd(';')};Database=YummyTests_{Guid.NewGuid():N};Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=5";
            Options = new DbContextOptionsBuilder<YummyDbContext>().UseSqlServer(_connectionString).Options;
        }

        public async Task InitializeAsync()
        {
            try
            {
                await using var db = CreateDbContext();
                await db.Database.EnsureCreatedAsync();
                SeedUsers(db);
                db.Users.AddRange(_extraUsers.Select(id => new AppUser { Id = id, UserName = $"user-{id:N}", Name = "Ek", Surname = "Kullanıcı", Email = $"{id:N}@test.com" }));
                db.DiningTables.Add(new DiningTable { TableNo = "Tek Masa", Capacity = 2 });
                await db.SaveChangesAsync();
                _isAvailable = true;
            }
            catch (SqlException)
            {
                _isAvailable = false;
            }
        }

        public async Task DisposeAsync()
        {
            if (!_isAvailable)
                return;

            await using var db = CreateDbContext();
            await db.Database.EnsureDeletedAsync();
        }

        [SkippableFact]
        public async Task ConcurrentBookingsForSameSlot_OnlyOneSucceeds()
        {
            Skip.IfNot(_isAvailable, "SQL Server (LocalDB) erişilebilir değil.");

            using var startSignal = new ManualResetEventSlim(false);

            var tasks = Enumerable.Range(0, ConcurrentRequests).Select(i => Task.Run(async () =>
            {
                startSignal.Wait();
                await using var db = CreateDbContext();
                await CreateReservationManager(db).AddReservationAsync((i % 2 == 0 ? UserA : UserB).ToString(), CreateDto(FutureDay));
            })).ToList();

            startSignal.Set();
            var results = await Task.WhenAll(tasks.Select(async t =>
            {
                try { await t; return (Exception?)null; }
                catch (Exception ex) { return ex; }
            }));

            Assert.Equal(1, results.Count(r => r == null));
            Assert.All(results.Where(r => r != null), ex => Assert.Equal("NoTable", Assert.IsType<LogicException>(ex).PropertyName));

            await using var check = CreateDbContext();
            Assert.Equal(1, await check.Reservations.CountAsync());
        }

        [SkippableFact]
        public async Task ConcurrentBookingsForDifferentDays_AllSucceed()
        {
            Skip.IfNot(_isAvailable, "SQL Server (LocalDB) erişilebilir değil.");

            var tasks = Enumerable.Range(0, 5).Select(i => Task.Run(async () =>
            {
                await using var db = CreateDbContext();
                await CreateReservationManager(db).AddReservationAsync(_extraUsers[i].ToString(), CreateDto(Today.AddDays(i + 1)));
            }));

            await Task.WhenAll(tasks);

            await using var check = CreateDbContext();
            Assert.Equal(5, await check.Reservations.CountAsync());
        }

        [SkippableFact]
        public async Task ConcurrentBookingsBySameUserForDifferentDays_CannotExceedActiveLimit()
        {
            Skip.IfNot(_isAvailable, "SQL Server (LocalDB) erişilebilir değil.");

            // farklı günler farklı gün kilitlerine düşer; limitin aşılmamasını kullanıcı kilidi sağlar.
            using var startSignal = new ManualResetEventSlim(false);
            var tasks = Enumerable.Range(1, 6).Select(day => Task.Run(async () =>
            {
                startSignal.Wait();
                await using var db = CreateDbContext();
                await CreateReservationManager(db).AddReservationAsync(UserA.ToString(), CreateDto(Today.AddDays(day)));
            })).ToList();

            startSignal.Set();
            var results = await Task.WhenAll(tasks.Select(async t =>
            {
                try { await t; return (Exception?)null; }
                catch (Exception ex) { return ex; }
            }));

            Assert.Equal(ReservationManager.MaxActiveReservationsPerUser, results.Count(r => r == null));
            Assert.All(results.Where(r => r != null), ex => Assert.Equal("ReservationLimit", Assert.IsType<LogicException>(ex).PropertyName));
        }
    }
}
