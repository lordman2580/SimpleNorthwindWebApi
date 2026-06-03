namespace SimpleNorthwind.Application.ApiLogs;

/// <summary>
/// 稽核紀錄唯讀檢視 DTO。<c>UserName</c> 由 JOIN employees 解析（user_id → 姓名），null 顯示「系統 / 匿名」。
/// HTTP method / path 由前端自 <c>ActionDetail</c> 解析呈現（前端只做呈現）。<c>ResponseResult</c> 為 JSON
/// 回應（nvarchar(max)），前端列上 substring、彈窗 beautify。
/// </summary>
public sealed record ApiLogDto(
    Guid Guid,
    int? UserId,
    string? UserName,
    string Actions,
    string? ActionDetail,
    int? ResponseStatus,
    string? ClientIp,
    int? DurationMs,
    string? ResponseResult,
    DateTime SummaryDate);

/// <summary>稽核查詢條件。Method 以 action_detail 前綴 LIKE 比對；OnlyErrors = response_status &gt;= 400；日期為 UTC。</summary>
public sealed record ApiLogQuery(
    int? UserId,
    string? Method,
    bool OnlyErrors,
    DateTime? FromUtc,
    DateTime? ToUtc,
    int Page,
    int PageSize);
