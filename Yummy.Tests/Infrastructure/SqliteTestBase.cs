using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Yummy.Data.Context;
using Yummy.Entity;

namespace Yummy.Tests.Infrastructure
{
    // her test sınıfı örneği (xUnit'te her test) kendi bellek içi SQLite veritabanıyla çalışır.
    // bağlantı açık kaldığı sürece veritabanı yaşar; farklı DbContext örnekleri aynı bağlantıyı paylaşarak ayrı HTTP isteklerini taklit eder.
    public abstract class SqliteTestBase : TestContext, IDisposable
    {
        protected static readonly Guid SmallTableId = Guid.NewGuid();  // 2 kişilik
        protected static readonly Guid MediumTableId = Guid.NewGuid(); // 4 kişilik
        protected static readonly DateTime FutureDay = DateTime.Today.AddDays(3);

        private readonly SqliteConnection _connection;
        protected override DbContextOptions<YummyDbContext> Options { get; }

        protected SqliteTestBase()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();
            Options = new DbContextOptionsBuilder<YummyDbContext>().UseSqlite(_connection).Options;

            using var db = CreateDbContext();
            db.Database.EnsureCreated();
            SeedUsers(db);
            db.DiningTables.AddRange(
                new DiningTable { DiningTableId = SmallTableId, TableNo = "Masa 1", Capacity = 2 },
                new DiningTable { DiningTableId = MediumTableId, TableNo = "Masa 2", Capacity = 4 });
            db.SaveChanges();
        }

        public void Dispose() => _connection.Dispose();
    }
}
