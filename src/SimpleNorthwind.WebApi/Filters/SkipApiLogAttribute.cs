namespace SimpleNorthwind.WebApi.Filters;

/// <summary>
/// 標註此屬性的 controller / action **不寫入** <c>api_logs</c>（連帶**不觸發** SignalR 推播）。
/// 用於讀稽核端點（<c>GET /api/apilogs</c>），避免「檢視稽核」自我留痕與即時推播回授（UD8）。
/// <c>ApiLogActionFilter</c> 由 endpoint metadata 偵測本屬性後整段跳過。
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public sealed class SkipApiLogAttribute : Attribute;
