using Microsoft.AspNetCore.SignalR;
using SimpleNorthwind.Application.ApiLogs;

namespace SimpleNorthwind.WebApi.Web.RealTime;

/// <summary>
/// <see cref="IApiLogBroadcaster"/> 的 SignalR 實作。以 <see cref="IHubContext{THub}"/>（thread-safe，
/// 可註冊為 singleton）向所有已連線者推送 <c>"apilog"</c> 事件。
/// <para>
/// Hub 與 broadcaster 都落在 WebApi（presentation）層，Application / Domain <b>零變動</b>（SignalR 不下沉）。
/// payload 直接沿用稽核 <see cref="ApiLogDto"/>（密碼 / token 已於寫入端 redact，見 06-稽核與共通技術規範 §2），
/// 不外洩敏感資料。
/// </para>
/// </summary>
public sealed class SignalRApiLogBroadcaster(IHubContext<ApiLogHub> hub) : IApiLogBroadcaster
{
    public Task PublishAsync(ApiLogDto log, CancellationToken ct = default)
        => hub.Clients.All.SendAsync("apilog", log, ct);
}
