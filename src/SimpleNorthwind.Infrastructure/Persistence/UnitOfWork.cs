using System.Data;
using Microsoft.Data.SqlClient;
using SimpleNorthwind.Application.Abstractions.Persistence;

namespace SimpleNorthwind.Infrastructure.Persistence;

/// <summary>
/// Scoped。連線延遲開啟；BeginAsync 開交易；Commit/Rollback 後清空交易。
/// Repository 一律使用 Connection（+ Transaction）。
/// </summary>
internal sealed class UnitOfWork(IDbConnectionFactory factory) : IUnitOfWork
{
    private SqlConnection? _connection;
    private SqlTransaction? _transaction;

    public IDbConnection Connection
    {
        get
        {
            _connection ??= factory.Create();
            if (_connection.State != ConnectionState.Open)
                _connection.Open();
            return _connection;
        }
    }

    public IDbTransaction? Transaction => _transaction;

    public async Task BeginAsync(CancellationToken ct = default)
    {
        _connection ??= factory.Create();
        if (_connection.State != ConnectionState.Open)
            await _connection.OpenAsync(ct).ConfigureAwait(false);

        _transaction = (SqlTransaction)await _connection.BeginTransactionAsync(ct).ConfigureAwait(false);
    }

    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (_transaction is null)
            return;

        await _transaction.CommitAsync(ct).ConfigureAwait(false);
        await _transaction.DisposeAsync().ConfigureAwait(false);
        _transaction = null;
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        if (_transaction is null)
            return;

        await _transaction.RollbackAsync(ct).ConfigureAwait(false);
        await _transaction.DisposeAsync().ConfigureAwait(false);
        _transaction = null;
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null)
            await _transaction.DisposeAsync().ConfigureAwait(false);
        if (_connection is not null)
            await _connection.DisposeAsync().ConfigureAwait(false);
    }
}
