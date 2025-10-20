using System.Text.Json;
using System.Text.Json.Serialization;

namespace Reshebnik.Web.Reshebnik.Web.Converters;

public class DateTimeUtcConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetDateTime();
        return TimeZoneHelper.ConvertToUtc(value);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var local = TimeZoneHelper.ConvertToUserTime(value.ToUniversalTime());
        writer.WriteStringValue(local);
    }
}

public class NullableDateTimeUtcConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;
        var value = reader.GetDateTime();
        return TimeZoneHelper.ConvertToUtc(value);
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            var local = TimeZoneHelper.ConvertToUserTime(value.Value.ToUniversalTime());
            writer.WriteStringValue(local);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}
