using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Web;

public class JsonStringEnumConverterTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private sealed record DayRecord(DayOfWeek Day);

    [Fact]
    public void Deserialize_StringEnumValue_DeserializesCorrectly()
    {
        var json = """{"Day":"Monday"}""";

        var result = JsonSerializer.Deserialize<DayRecord>(json, Options);

        result.Should().NotBeNull();
        result!.Day.Should().Be(DayOfWeek.Monday);
    }

    [Fact]
    public void Deserialize_CaseInsensitiveString_DeserializesCorrectly()
    {
        var json = """{"Day":"monday"}""";

        var result = JsonSerializer.Deserialize<DayRecord>(json, Options);

        result.Should().NotBeNull();
        result!.Day.Should().Be(DayOfWeek.Monday);
    }

    [Fact]
    public void Serialize_EnumValue_SerializesAsString()
    {
        var record = new DayRecord(DayOfWeek.Monday);

        var json = JsonSerializer.Serialize(record, Options);

        json.Should().Contain("\"Monday\"");
        json.Should().NotContain("1");
    }

    [Fact]
    public void Deserialize_IntegerValue_DeserializesCorrectly()
    {
        var json = """{"Day":1}""";

        var result = JsonSerializer.Deserialize<DayRecord>(json, Options);

        result.Should().NotBeNull();
        result!.Day.Should().Be(DayOfWeek.Monday);
    }
}
