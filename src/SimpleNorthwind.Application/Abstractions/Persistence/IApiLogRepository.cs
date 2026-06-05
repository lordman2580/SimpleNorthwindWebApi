using SimpleNorthwind.Application.ApiLogs;
using SimpleNorthwind.Application.Common;

namespace SimpleNorthwind.Application.Abstractions.Persistence;

/// <summary>
/// API 稽核寫入 + 讀取。寫入使用獨立短連線（不參與業務交易）→ 即使業務 rollback 稽核仍留存。
/// 敏感欄位（密碼 / token）由呼叫端 redact 後才傳入 detail / responseResult。
/// </summary>
public interface IApiLogRepository
{
    Task WriteAsync(Guid id, int? userId, string actions, string? detail,
        int? responseStatus, string? responseResult, string? clientIp, int? durationMs,
        DateTime summaryDateUtc, CancellationToken ct = default);

    /// <summary>分頁查詢稽核紀錄（JOIN employees 解析操作者姓名、過濾、summary_date DESC）。供稽核檢視頁。</summary>
    Task<PagedResult<ApiLogDto>> QueryAsync(ApiLogQuery query, CancellationToken ct = default);
}
