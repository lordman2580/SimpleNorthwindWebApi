using SimpleNorthwind.Application.ApiLogs;
using SimpleNorthwind.Application.Common;

namespace SimpleNorthwind.Application.Abstractions.Services;

public interface IApiLogQueryService
{
    /// <summary>分頁查詢稽核紀錄（過濾 + 操作者姓名解析）。</summary>
    Task<PagedResult<ApiLogDto>> QueryAsync(ApiLogQuery query, CancellationToken ct = default);
}
