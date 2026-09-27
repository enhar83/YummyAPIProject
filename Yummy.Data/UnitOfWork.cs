using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Yummy.Core.IUnitOfWork;
using Yummy.Data.Context;

namespace Yummy.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly YummyDbContext _context;

        public UnitOfWork(YummyDbContext context)
        {
            _context = context;
        }

        public async Task<int> SaveAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public Task ExecuteInLockedTransactionAsync(string lockKey, Func<Task> action, CancellationToken cancellationToken = default) =>
            ExecuteInLockedTransactionAsync(new[] { lockKey }, action, cancellationToken);

        public async Task ExecuteInLockedTransactionAsync(IReadOnlyList<string> lockKeys, Func<Task> action, CancellationToken cancellationToken = default)
        {
            // await using önemlidir; metot nasıl biterse bitsin, transaction nesnesi otomatik olarak kapatılır. 
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            // sp_getapplock: SQL Server'ın uygulama seviyesindeki kilidi. ismi olan bir kilit alır. bu kilidin herhangi bir rabloya veya satıra bağlı olması gerekmez.
            if (_context.Database.IsSqlServer())
            {
                foreach (var lockKey in lockKeys)
                {
                    await _context.Database.ExecuteSqlInterpolatedAsync($@"
                        DECLARE @result int;
                        EXEC @result = sp_getapplock @Resource = {lockKey}, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000;
                        IF @result < 0 THROW 50000, 'Kaynak kilidi alınamadı.', 1;", cancellationToken);

                    //resource: anahtarın adı. reservation: 2026-09-29 vs.
                    //lockmode: kilit türü. Exclusive: başka kimse alamaz. Update: başkası okuyabilir ama yazamaz. Shared: başkası okuyabilir ama yazamaz.
                    //lockowner: kilidin sahibi. Transaction: transaction bitince kilit de biter. Session: transaction bitince kilit kalır.
                    //locktimeout: milisaniye cinsinden kilit alma süresi. 10000 = 10 saniye. kilit alınamazsa hata fırlatılır.

                    /* sp_getapplock döndürdüğü değerler:
                        - 0: kilit hemen alındı
                        - 1: bir süre bekledikten sonra alındı.
                        - -1: 10 saniye doldu alınamadı
                        - -2, -3, -999: iptal edildi / deadlock / başka bir hata
                     */

                    // IF @result < 0 THROW ... , kilit alınamadıysa hata fırlat. Bu hata C# tarafına exception olarak gelir; işlem yapılmaz ve transaction geri alınır. 

                    // EF Core {lockKey} değerini SQL'e parametre olarak gönderir. SQL injection riski yoktur.
                }
            }

            await action(); // kilit artık elimizde. managerın verdiği iş çalışır: oku, kontrol et, sepete koy, SaveAsync.
            await transaction.CommitAsync(cancellationToken); // yazılanlar kalıcı hale gelir ve kilitler otomatik bırakılır (mehmet iş yapabilir).
        }

        public async ValueTask DisposeAsync()
        {
            await _context.DisposeAsync();
        }
    }
}

/* neden birden fazla kilit alınıyor?

    kullanıcı kilidi ve gün kilidi birlikte alınır. 
        - gün kilidi aynı masanın iki kişiye verilmesini engeller.
        - kullanıcı kilidi aynı kullanıcının farklı günlere aynı anda istek atarak 3 rezervasyon limitini aşmasını engeller.
*/

// kilit isimleri ise managerlar içerisinde üretilmektedir.
// örn: private static string GetDateLockKey (DateTime date) => $"reservation:{date:yyyy-MM-dd}";
// örn: private static string GetUserLockKey (Guid userId) => $"reservation-user:{userId}";
// kısaca UoW içerisindeki {lockKey} bir boşluk gibi. O boşluğu dolduran değer manager içerisinden geliyor.

// manager içerisinde private olarak Lock metodları tanımlanıyor ve UoW içerisindeki {lockKey} parametresine gönderiliyor.
// manager içerisinde await ile lock başlatılıyor ve sıkıntılı işlemler lock içerisinde güvenle yapılabiliyor.