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

        public async Task ExecuteInLockedTransactionAsync(string lockKey, Func<Task> action, CancellationToken cancellationToken = default)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            // sp_getapplock: SQL Server'ın uygulama seviyesindeki kilidi. LockOwner = 'Transaction' olduğu için kilit commit/rollback ile birlikte otomatik bırakılır.
            // kilit 10 saniye içinde alınamazsa (dönüş değeri < 0) hata fırlatılır ve transaction geri alınır.
            // SQL Server dışındaki provider'larda (örn. testlerdeki SQLite) yazma işlemleri zaten veritabanı seviyesinde sıralandığı için bu adım atlanır.
            if (_context.Database.IsSqlServer())
            {
                await _context.Database.ExecuteSqlInterpolatedAsync($@"
                    DECLARE @result int;
                    EXEC @result = sp_getapplock @Resource = {lockKey}, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000;
                    IF @result < 0 THROW 50000, 'Kaynak kilidi alınamadı.', 1;", cancellationToken);
            }

            await action();
            await transaction.CommitAsync(cancellationToken);
        }

        public async ValueTask DisposeAsync()
        {
            await _context.DisposeAsync();
        }
    }
}
