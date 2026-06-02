using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using SimpleNorthwind.Infrastructure.Time;

namespace SimpleNorthwind.Infrastructure.Serialization;

/// <summary>DateTime? 版本：null 透傳，其餘同 <see cref="ClientLocalDateTimeJsonConverter"/>。</summary>
public sealed class NullableClientLocalDateTimeJsonConverter : JsonConverter<DateTime?>
{
    private const string Format = "yyyy-MM-dd HH:mm:ss";

    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        var text = reader.GetString();
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var local = DateTime.ParseExact(text, Format, CultureInfo.InvariantCulture);
        return TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(local, DateTimeKind.Unspecified), ClientTimeZoneAccessor.Current);
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        var utc = DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);
        var local = TimeZoneInfo.ConvertTimeFromUtc(utc, ClientTimeZoneAccessor.Current);
        writer.WriteStringValue(local.ToString(Format, CultureInfo.InvariantCulture));
    }
}
