using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Yummy.Data.Context;

namespace Yummy.Tests.Infrastructure
{
    // sp_getapplock SQL Server'a özgü olduğu için kilit testleri gerçek bir SQL Server (varsayılan: LocalDB) üzerinde,
    // her test için oluşturulup sonunda silinen geçici bir veritabanında koşar. SQL Server'a erişilemezse testler atlanır (Skipped).
    // farklı bir sunucu kullanmak için YUMMY_TEST_SQLSERVER ortam değişkenine "Server=...;" kısmı verilebilir.
    public abstract class SqlServerTestBase : TestContext, IAsyncLifetime
    {
        protected const string SkipReason = "SQL Server (LocalDB) erişilebilir değil.";

        protected bool IsAvailable { get; private set; }
        protected override DbContextOptions<YummyDbContext> Options { get; }

        protected SqlServerTestBase()
        {
            var server = System.Environment.GetEnvironmentVariable("YUMMY_TEST_SQLSERVER") ?? @"Server=(localdb)\MSSQLLocalDB;";
            var connectionString = $"{server.TrimEnd(';')};Database=YummyTests_{Guid.NewGuid():N};Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=5";
            Options = new DbContextOptionsBuilder<YummyDbContext>().UseSqlServer(connectionString).Options;
        }

        // test sınıfına özel başlangıç verisi.
        protected virtual Task SeedAsync(YummyDbContext db) => Task.CompletedTask;

        public async Task InitializeAsync()
        {
            try
            {
                await using var db = CreateDbContext();
                await db.Database.EnsureCreatedAsync();
                SeedUsers(db);
                await SeedAsync(db);
                await db.SaveChangesAsync();
                IsAvailable = true;
            }
            catch (SqlException)
            {
                IsAvailable = false;
            }
        }

        public async Task DisposeAsync()
        {
            if (!IsAvailable)
                return;

            await using var db = CreateDbContext();
            await db.Database.EnsureDeletedAsync();
        }

        // tüm görevleri aynı anda başlatır ve her birinin sonucunu (başarılıysa null, değilse fırlattığı hata) döner.
        protected static async Task<Exception?[]> RunConcurrentlyAsync(IEnumerable<Func<Task>> actions)
        {
            using var startSignal = new ManualResetEventSlim(false);
            var tasks = actions.Select(action => Task.Run(async () =>
            {
                startSignal.Wait();
                await action();
            })).ToList();

            startSignal.Set();
            return await Task.WhenAll(tasks.Select(async t =>
            {
                try { await t; return (Exception?)null; }
                catch (Exception ex) { return ex; }
            }));
        }
    }
}
