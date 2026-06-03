using System.Text.Json;
using Shouldly;
using SimpleNorthwind.Infrastructure.Serialization;
using SimpleNorthwind.Infrastructure.Time;

namespace SimpleNorthwind.Infrastructure.UnitTests;

public class ClientLocalDateTimeJsonConverterTests
{
    // A custom +08:00 timezone that works on every OS without relying on a
    // system-registered id such as "China Standard Time" or "Asia/Taipei".
    private static readonly TimeZoneInfo Utc8 =
        TimeZoneInfo.CreateCustomTimeZone("ut+8", TimeSpan.FromHours(8), "ut+8", "ut+8");

    private static JsonSerializerOptions BuildOptions() =>
        new() { Converters = { new ClientLocalDateTimeJsonConverter() } };

    private static JsonSerializerOptions BuildNullableOptions() =>
        new() { Converters = { new NullableClientLocalDateTimeJsonConverter() } };

    // ── helper record so the converter is applied to a named property ──────
    private record Wrapper(DateTime When);
    private record NullableWrapper(DateTime? When);

    // ── ClientLocalDateTimeJsonConverter ───────────────────────────────────

    [Fact]
    public void Serialize_UtcDateTime_WritesLocalTime()
    {
        ClientTimeZoneAccessor.Current = Utc8;
        var opts = BuildOptions();

        // 2024-01-02 00:00:00 UTC  →  2024-01-02 08:00:00 +08
        var utcDt = DateTime.SpecifyKind(new DateTime(2024, 1, 2, 0, 0, 0), DateTimeKind.Utc);
        var json = JsonSerializer.Serialize(new Wrapper(utcDt), opts);

        json.ShouldContain("2024-01-02 08:00:00");
    }

    [Fact]
    public void Deserialize_LocalTimeString_ReturnsUtcDateTime()
    {
        ClientTimeZoneAccessor.Current = Utc8;
        var opts = BuildOptions();

        var expectedUtc = DateTime.SpecifyKind(new DateTime(2024, 1, 2, 0, 0, 0), DateTimeKind.Utc);
        var json = """{"When":"2024-01-02 08:00:00"}""";

        var result = JsonSerializer.Deserialize<Wrapper>(json, opts)!;

        result.When.ShouldBe(expectedUtc);
    }

    [Fact]
    public void RoundTrip_SerializeDeserialize_PreservesUtcValue()
    {
        ClientTimeZoneAccessor.Current = Utc8;
        var opts = BuildOptions();

        var original = DateTime.SpecifyKind(new DateTime(2024, 6, 15, 10, 30, 0), DateTimeKind.Utc);
        var json = JsonSerializer.Serialize(new Wrapper(original), opts);
        var result = JsonSerializer.Deserialize<Wrapper>(json, opts)!;

        result.When.ShouldBe(original);
    }

    // ── NullableClientLocalDateTimeJsonConverter ───────────────────────────

    [Fact]
    public void NullableConverter_SerializeNull_WritesJsonNull()
    {
        ClientTimeZoneAccessor.Current = Utc8;
        var opts = BuildNullableOptions();

        var json = JsonSerializer.Serialize(new NullableWrapper(null), opts);

        json.ShouldContain("null");
    }

    [Fact]
    public void NullableConverter_DeserializeNull_ReturnsNull()
    {
        ClientTimeZoneAccessor.Current = Utc8;
        var opts = BuildNullableOptions();

        var result = JsonSerializer.Deserialize<NullableWrapper>("""{"When":null}""", opts)!;

        result.When.ShouldBeNull();
    }

    [Fact]
    public void NullableConverter_SerializeValue_WritesLocalTime()
    {
        ClientTimeZoneAccessor.Current = Utc8;
        var opts = BuildNullableOptions();

        var utcDt = DateTime.SpecifyKind(new DateTime(2024, 1, 2, 0, 0, 0), DateTimeKind.Utc);
        var json = JsonSerializer.Serialize(new NullableWrapper(utcDt), opts);

        json.ShouldContain("2024-01-02 08:00:00");
    }

    [Fact]
    public void NullableConverter_RoundTrip_PreservesUtcValue()
    {
        ClientTimeZoneAccessor.Current = Utc8;
        var opts = BuildNullableOptions();

        DateTime? original = DateTime.SpecifyKind(new DateTime(2024, 6, 15, 10, 30, 0), DateTimeKind.Utc);
        var json = JsonSerializer.Serialize(new NullableWrapper(original), opts);
        var result = JsonSerializer.Deserialize<NullableWrapper>(json, opts)!;

        result.When.ShouldBe(original);
    }
}
