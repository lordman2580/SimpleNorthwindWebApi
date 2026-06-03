namespace SimpleNorthwind.Application.Abstractions.Persistence;

/// <summary>
/// API 稽核寫入。使用獨立短連線（不參與業務交易）→ 即使業務 rollback 稽核仍留存。
/// 敏感欄位（密碼 / token）由呼叫端 redact 後才傳入 detail / responseResult。
/// </summary>
public interface IApiLogRepository
{
    Task WriteAsync(Guid id, int? userId, string actions, string? detail,
        int? responseStatus, string? responseResult, DateTime summaryDateUtc, CancellationToken ct = default);
}
