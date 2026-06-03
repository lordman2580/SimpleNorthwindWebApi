using Dapper;
using SimpleNorthwind.Application.Abstractions.Persistence;
using SimpleNorthwind.Domain.Entities;
using SimpleNorthwind.Infrastructure.Persistence;
using static SimpleNorthwind.Infrastructure.Persistence.SqlNaming;

namespace SimpleNorthwind.Infrastructure.Repositories;

/// <summary>使用獨立短連線寫入（不參與業務交易）→ 即使業務 rollback，稽核仍留存。</summary>
internal sealed class ApiLogRepository(IDbConnectionFactory factory) : IApiLogRepository
{
    private static readonly string[] InsertProps =
    [
        nameof(ApiLog.Guid), nameof(ApiLog.UserId), nameof(ApiLog.Actions), nameof(ApiLog.ActionDetail),
        nameof(ApiLog.ResponseStatus), nameof(ApiLog.ResponseResult), nameof(ApiLog.SummaryDate),
    ];

    private static readonly string InsertSql =
        $"INSERT INTO dbo.api_logs ({Cols(InsertProps)}) VALUES ({Params(InsertProps)});";

    public async Task WriteAsync(Guid id, int? userId, string actions, string? detail,
        int? responseStatus, string? responseResult, DateTime summaryDateUtc, CancellationToken ct = default)
    {
        var entity = new ApiLog
        {
            Guid = id,
            UserId = userId,
            Actions = actions,
            ActionDetail = detail,
            ResponseStatus = responseStatus,
            ResponseResult = responseResult,
            SummaryDate = summaryDateUtc,
        };

        await using var connection = factory.Create();
        await connection.OpenAsync(ct).ConfigureAwait(false);
        await connection.ExecuteAsync(
            new CommandDefinition(InsertSql, entity, cancellationToken: ct)).ConfigureAwait(false);
    }
}
