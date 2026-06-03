using SimpleNorthwind.Application.Abstractions.Persistence;
using SimpleNorthwind.Application.Abstractions.Services;
using SimpleNorthwind.Application.ApiLogs;
using SimpleNorthwind.Application.Common;

namespace SimpleNorthwind.Application.Services;

/// <summary>稽核紀錄唯讀查詢（薄封裝；過濾 / 分頁 / 姓名解析在 repository 的單一 SQL）。</summary>
public sealed class ApiLogQueryService(IApiLogRepository apiLogs) : IApiLogQueryService
{
    public Task<PagedResult<ApiLogDto>> QueryAsync(ApiLogQuery query, CancellationToken ct = default) =>
        apiLogs.QueryAsync(query, ct);
}
