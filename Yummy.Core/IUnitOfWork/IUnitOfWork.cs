using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yummy.Core.IUnitOfWork
{
    public interface IUnitOfWork : IAsyncDisposable
    {
        Task<int> SaveAsync(CancellationToken cancellationToken = default);

        // action, lockKey için alınan özel (exclusive) bir kilit altında tek bir transaction içerisinde çalıştırılır.
        // aynı lockKey ile gelen eşzamanlı istekler sıraya girer; "kontrol et → kaydet" adımları arasına başka bir istek giremez.
        Task ExecuteInLockedTransactionAsync(string lockKey, Func<Task> action, CancellationToken cancellationToken = default);
    }
}
