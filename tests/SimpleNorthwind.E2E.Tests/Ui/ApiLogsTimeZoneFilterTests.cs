using System.Net;
using Shouldly;

namespace SimpleNorthwind.E2E.Tests;

/// <summary>
/// 稽核頁日期區間過濾的時區轉換（迴歸測試）。
/// <para>
/// 後端 <c>summary_date</c> 存 UTC、SQL 直接以 <c>@fromUtc</c>/<c>@toUtc</c> 比對；UI 的 from/to 為
/// <b>瀏覽器本地時間</b>，<c>ApiLogsUiController</c> 須先 local→UTC 再送 loopback API。無 <c>tz</c> cookie 時
/// 退回 <c>App:DefaultTimeZone</c> = Asia/Taipei（+8），與既有 <c>TimeZoneTests</c> 慣例一致。
/// </para>
/// <para>
/// 原 bug：query-string 的 <see cref="DateTime"/> 走預設 model binding、不經 <c>ClientLocalDateTimeJsonConverter</c>，
/// UI 又未自行轉換，導致本地時間被當成 UTC，使區間撈不到（或誤撈）資料。下列兩案在「修正前 / 後」結果相反，
/// 同時覆蓋 from 與 to 的轉換邊界。
/// </para>
/// </summary>
[Collection(E2ECollection.Name)]
public sealed class ApiLogsTimeZoneFilterTests(CustomWebApplicationFactory factory)
{
    private const string ProbePath = "/api/__tz_probe__";

    // 探針事件 summary_date = 2024-06-01 01:00:00 UTC（= Asia/Taipei 本地 09:00）。
    private static readonly DateTime ProbeUtc = new(2024, 6, 1, 1, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task FromFilter_LocalTimeConvertedToUtc_IncludesEarlierUtcRow()
    {
        await factory.ClearApiLogsAsync();
        await factory.InsertApiLogAsync($"GET {ProbePath}", ProbeUtc);
        var client = await factory.LoginUiAsync();

        // from 本地 08:00 (Taipei) → UTC 00:00；探針 01:00 UTC ≥ 00:00 → 應出現。
        // 修正前（未轉換）：08:00 被當 UTC，01:00 < 08:00 → 不出現。
        var html = await GetHtmlAsync(client, "/apilogs?from=2024-06-01T08%3A00");

        html.ShouldContain(ProbePath);
    }

    [Fact]
    public async Task ToFilter_LocalTimeConvertedToUtc_ExcludesLaterUtcRow()
    {
        await factory.ClearApiLogsAsync();
        await factory.InsertApiLogAsync($"GET {ProbePath}", ProbeUtc);
        var client = await factory.LoginUiAsync();

        // to 本地 08:00 (Taipei) → UTC 00:00；探針本地時間為 09:00（晚於 08:00）→ 不應出現。
        // 修正前（未轉換）：08:00 被當 UTC，01:00 ≤ 08:00 → 誤出現。
        var html = await GetHtmlAsync(client, "/apilogs?to=2024-06-01T08%3A00");

        html.ShouldNotContain(ProbePath);
    }

    private static async Task<string> GetHtmlAsync(HttpClient client, string path)
    {
        var res = await client.GetAsync(path);
        res.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await res.Content.ReadAsStringAsync();
    }
}
