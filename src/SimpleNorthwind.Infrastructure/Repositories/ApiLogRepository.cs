using Dapper;
using SimpleNorthwind.Application.Abstractions.Persistence;
using SimpleNorthwind.Application.ApiLogs;
using SimpleNorthwind.Application.Common;
using SimpleNorthwind.Domain.Entities;
using SimpleNorthwind.Infrastructure.Persistence;
using static SimpleNorthwind.Infrastructure.Persistence.SqlNaming;

namespace SimpleNorthwind.Infrastructure.Repositories;

/// <summary>使用獨立短連線（不參與業務交易）→ 即使業務 rollback，稽核仍留存。讀取亦用獨立短連線。</summary>
internal sealed class ApiLogRepository(IDbConnectionFactory factory) : IApiLogRepository
{
    private static readonly string[] InsertProps =
    [
        nameof(ApiLog.Guid), nameof(ApiLog.UserId), nameof(ApiLog.Actions), nameof(ApiLog.ActionDetail),
        nameof(ApiLog.ResponseStatus), nameof(ApiLog.ResponseResult), nameof(ApiLog.ClientIp), nameof(ApiLog.DurationMs),
        nameof(ApiLog.SummaryDate),
    ];

    private static readonly string InsertSql =
        $"INSERT INTO dbo.api_logs ({Cols(InsertProps)}) VALUES ({Params(InsertProps)});";

    private static readonly string Where =
        $"""
        WHERE (@userId IS NULL OR l.{Col(nameof(ApiLog.UserId))} = @userId)
          AND (@methodLike IS NULL OR l.{Col(nameof(ApiLog.ActionDetail))} LIKE @methodLike)
          AND (@onlyErrors = 0 OR l.{Col(nameof(ApiLog.ResponseStatus))} >= 400)
          AND (@fromUtc IS NULL OR l.{Col(nameof(ApiLog.SummaryDate))} >= @fromUtc)
          AND (@toUtc IS NULL OR l.{Col(nameof(ApiLog.SummaryDate))} <= @toUtc)
        """;

    private static readonly string CountSql = $"SELECT COUNT(*) FROM dbo.api_logs l {Where};";

    private static readonly string PageSql =
        $"""
        SELECT l.{Col(nameof(ApiLog.Guid))}, l.{Col(nameof(ApiLog.UserId))},
               (e.{Col(nameof(Employee.FirstName))} + ' ' + e.{Col(nameof(Employee.LastName))}) AS {Col(nameof(ApiLogDto.UserName))},
               l.{Col(nameof(ApiLog.Actions))}, l.{Col(nameof(ApiLog.ActionDetail))}, l.{Col(nameof(ApiLog.ResponseStatus))},
               l.{Col(nameof(ApiLog.ClientIp))}, l.{Col(nameof(ApiLog.DurationMs))}, l.{Col(nameof(ApiLog.ResponseResult))}, l.{Col(nameof(ApiLog.SummaryDate))}
        FROM dbo.api_logs l
        LEFT JOIN dbo.employees e ON e.{Col(nameof(Employee.EmployeeId))} = l.{Col(nameof(ApiLog.UserId))}
        {Where}
        ORDER BY l.{Col(nameof(ApiLog.SummaryDate))} DESC
        OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;
        """;

    public async Task WriteAsync(Guid id, int? userId, string actions, string? detail,
        int? responseStatus, string? responseResult, string? clientIp, int? durationMs,
        DateTime summaryDateUtc, CancellationToken ct = default)
    {
        var entity = new ApiLog
        {
            Guid = id,
            UserId = userId,
            Actions = actions,
            ActionDetail = detail,
            ResponseStatus = responseStatus,
            ResponseResult = responseResult,
            ClientIp = clientIp,
            DurationMs = durationMs,
            SummaryDate = summaryDateUtc,
        };

        await using var connection = factory.Create();
        await connection.OpenAsync(ct).ConfigureAwait(false);
        await connection.ExecuteAsync(
            new CommandDefinition(InsertSql, entity, cancellationToken: ct)).ConfigureAwait(false);
    }

    public async Task<PagedResult<ApiLogDto>> QueryAsync(ApiLogQuery query, CancellationToken ct = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize is < 1 or > 100 ? 15 : query.PageSize;
        var args = new
        {
            userId = query.UserId,
            methodLike = string.IsNullOrWhiteSpace(query.Method) ? (string?)null : query.Method + " %",
            onlyErrors = query.OnlyErrors ? 1 : 0,
            fromUtc = query.FromUtc,
            toUtc = query.ToUtc,
            offset = (page - 1) * pageSize,
            pageSize,
        };

        await using var connection = factory.Create();
        await connection.OpenAsync(ct).ConfigureAwait(false);

        // 單一連線不可並行 → 依序 await（先 count 再 page）。
        var total = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(CountSql, args, cancellationToken: ct)).ConfigureAwait(false);
        var items = (await connection.QueryAsync<ApiLogDto>(
            new CommandDefinition(PageSql, args, cancellationToken: ct)).ConfigureAwait(false)).ToList();
        return new PagedResult<ApiLogDto>(items, page, pageSize, total);
    }
}
