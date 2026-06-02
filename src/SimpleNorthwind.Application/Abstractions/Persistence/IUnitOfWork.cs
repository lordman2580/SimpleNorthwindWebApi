using System.Data;

namespace SimpleNorthwind.Application.Abstractions.Persistence;

/// <summary>
/// 管理單一連線與交易。Repository 一律使用 Connection（+ Transaction）執行 Dapper。
/// 連線延遲開啟；未呼叫 BeginAsync 時 Transaction 為 null（單語句自動 commit）。
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
{
    IDbConnection Connection { get; }
    IDbTransaction? Transaction { get; }

    Task BeginAsync(CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}
