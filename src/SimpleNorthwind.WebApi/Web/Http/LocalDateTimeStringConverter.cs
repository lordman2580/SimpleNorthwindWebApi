using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimpleNorthwind.WebApi.Web.Http;

/// <summary>
/// loopback client 端 DateTime 轉換：API 已將日期輸出為呼叫端本地時區的 <c>"yyyy-MM-dd HH:mm:ss"</c>
/// 字串（非 ISO，見既有 <c>ClientLocalDateTimeJsonConverter</c>）。此 converter **原樣**解析 / 輸出該格式
/// （不做時區運算，<c>Kind=Unspecified</c>），使 UI 直接顯示；提交時 API 端再以 X-Time-Zone 轉回 UTC。
/// </summary>
public sealed class LocalDateTimeStringConverter : JsonConverter<DateTime>
{
    internal const string Format = "yyyy-MM-dd HH:mm:ss";

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => DateTime.ParseExact(
            reader.GetString() ?? throw new JsonException("日期字串不可為 null。"),
            Format, CultureInfo.InvariantCulture);

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString(Format, CultureInfo.InvariantCulture));
}

/// <summary><see cref="LocalDateTimeStringConverter"/> 的 nullable 版本：null 透傳。</summary>
public sealed class NullableLocalDateTimeStringConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;
        var text = reader.GetString();
        return string.IsNullOrWhiteSpace(text)
            ? null
            : DateTime.ParseExact(text, LocalDateTimeStringConverter.Format, CultureInfo.InvariantCulture);
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value is null)
            writer.WriteNullValue();
        else
            writer.WriteStringValue(value.Value.ToString(LocalDateTimeStringConverter.Format, CultureInfo.InvariantCulture));
    }
}
