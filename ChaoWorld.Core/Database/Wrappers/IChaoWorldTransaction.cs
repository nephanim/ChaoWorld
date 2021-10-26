using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace ChaoWorld.Core
{
    public interface IChaoWorldTransaction: IDbTransaction, IAsyncDisposable
    {
        public Task CommitAsync(CancellationToken ct = default);
        public Task RollbackAsync(CancellationToken ct = default);
    }
}