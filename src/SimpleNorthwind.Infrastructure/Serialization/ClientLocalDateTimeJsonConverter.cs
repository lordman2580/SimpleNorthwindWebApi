using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using SimpleNorthwind.Infrastructure.Time;

namespace SimpleNorthwind.Infrastructure.Serialization;

/// <summary>
/// DateTime 雙向轉換：輸出將 DB 的 UTC → 呼叫端本地時區並格式化；輸入視為呼叫端本地時間 → 轉回 UTC。
/// 時區取自 <see cref="ClientTimeZoneAccessor.Current"/>。見 06-稽核與共通技術規範#3。
/// </summary>
public sealed class ClientLocalDateTimeJsonConverter : JsonConverter<DateTime>
{
    private const string Format = "yyyy-MM-dd HH:mm:ss";

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var text = reader.GetString() ?? throw new JsonException("日期字串不可為 null。");
        var local = DateTime.ParseExact(text, Format, CultureInfo.InvariantCulture);
        return TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(local, DateTimeKind.Unspecified), ClientTimeZoneAccessor.Current);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var utc = DateTime.SpecifyKind(value, DateTimeKind.Utc);
        var local = TimeZoneInfo.ConvertTimeFromUtc(utc, ClientTimeZoneAccessor.Current);
        writer.WriteStringValue(local.ToString(Format, CultureInfo.InvariantCulture));
    }
}
