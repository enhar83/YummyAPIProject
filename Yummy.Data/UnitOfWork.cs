using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public async ValueTask DisposeAsync()
        {
            await _context.DisposeAsync();
        }
    }
}
