using SimpleNorthwind.Application.ApiLogs;

namespace SimpleNorthwind.WebApi.Web.RealTime;

/// <summary>
/// 即時稽核推播抽象：稽核寫入成功後由 <see cref="Filters.ApiLogActionFilter"/> 呼叫，將該筆
/// <see cref="ApiLogDto"/> 推送給正在檢視 <c>/apilogs</c> 的瀏覽器。
/// <para>
/// 以介面隔離 SignalR —— filter 不直接依賴 <c>IHubContext</c>，測試可換 no-op 實作（[[26-即時稽核推播]] §4）。
/// 推播為稽核的「附帶動作」，失敗<b>不得</b>影響業務回應（由呼叫端 try/catch 保護）。
/// </para>
/// </summary>
public interface IApiLogBroadcaster
{
    /// <summary>推送一筆稽核紀錄給所有已連線的檢視者（事件名 <c>"apilog"</c>）。</summary>
    Task PublishAsync(ApiLogDto log, CancellationToken ct = default);
}
