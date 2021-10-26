using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

using Npgsql;

namespace ChaoWorld.Core
{
    public interface IChaoWorldConnection: IDbConnection, IAsyncDisposable
    {
        public Guid ConnectionId { get; }

        public Task OpenAsync(CancellationToken cancellationToken = default);
        public Task CloseAsync();

        public Task ChangeDatabaseAsync(string databaseName, CancellationToken ct = default);

        public ValueTask<IChaoWorldTransaction> BeginTransactionAsync(CancellationToken ct = default) => BeginTransactionAsync(IsolationLevel.Unspecified, ct);
        public ValueTask<IChaoWorldTransaction> BeginTransactionAsync(IsolationLevel level, CancellationToken ct = default);

        public NpgsqlBinaryImporter BeginBinaryImport(string copyFromCommand);
        public NpgsqlBinaryExporter BeginBinaryExport(string copyToCommand);

        [Obsolete] new void Open();
        [Obsolete] new void Close();

        [Obsolete] new IDbTransaction BeginTransaction();
        [Obsolete] new IDbTransaction BeginTransaction(IsolationLevel il);
    }
}