using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yummy.Core.IUnitOfWork
{
    public interface IUnitOfWork : IAsyncDisposable
    {
        Task<int> SaveAsync();
    }
}
