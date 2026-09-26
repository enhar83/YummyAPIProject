using Yummy.Core.IUnitOfWork;

namespace Yummy.Tests.Infrastructure
{
    // eşzamanlı bir isteği deterministik olarak taklit eder: işlem kilidi almadan hemen önce (yani ilk okumayı yapmış, kilidi bekliyorken)
    // başka bir isteğin işi araya sokulur. gerçek hayatta bu, kilidi o an başka bir isteğin tutmasına karşılık gelir.
    public class InterleavingUnitOfWork : IUnitOfWork
    {
        private readonly IUnitOfWork _inner;
        private Func<Task>? _interleave;

        public InterleavingUnitOfWork(IUnitOfWork inner, Func<Task> interleave)
        {
            _inner = inner;
            _interleave = interleave;
        }

        public Task<int> SaveAsync(CancellationToken cancellationToken = default) => _inner.SaveAsync(cancellationToken);

        public async Task ExecuteInLockedTransactionAsync(string lockKey, Func<Task> action, CancellationToken cancellationToken = default)
        {
            await RunInterleaveOnceAsync();
            await _inner.ExecuteInLockedTransactionAsync(lockKey, action, cancellationToken);
        }

        public async Task ExecuteInLockedTransactionAsync(IReadOnlyList<string> lockKeys, Func<Task> action, CancellationToken cancellationToken = default)
        {
            await RunInterleaveOnceAsync();
            await _inner.ExecuteInLockedTransactionAsync(lockKeys, action, cancellationToken);
        }

        public ValueTask DisposeAsync() => _inner.DisposeAsync();

        private async Task RunInterleaveOnceAsync()
        {
            var interleave = _interleave;
            _interleave = null;
            if (interleave != null)
                await interleave();
        }
    }
}
