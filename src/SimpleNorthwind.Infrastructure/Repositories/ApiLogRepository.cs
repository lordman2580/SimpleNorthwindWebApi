using Dapper;
using SimpleNorthwind.Application.Abstractions.Persistence;
using SimpleNorthwind.Infrastructure.Persistence;

namespace SimpleNorthwind.Infrastructure.Repositories;

/// <summary>使用獨立短連線寫入（不參與業務交易）→ 即使業務 rollback，稽核仍留存。</summary>
internal sealed class ApiLogRepository(IDbConnectionFactory factory) : IApiLogRepository
{
    public async Task WriteAsync(Guid id, int? userId, string actions, string? detail, DateTime summaryDateUtc, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO dbo.api_logs (guid, user_id, actions, action_detail, summary_date)
            VALUES (@id, @userId, @actions, @detail, @summaryDateUtc);
            """;
        await using var connection = factory.Create();
        await connection.OpenAsync(ct).ConfigureAwait(false);
        await connection.ExecuteAsync(
            new CommandDefinition(sql, new { id, userId, actions, detail, summaryDateUtc }, cancellationToken: ct)).ConfigureAwait(false);
    }
}
