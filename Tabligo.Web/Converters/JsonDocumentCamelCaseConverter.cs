using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tabligo.Web.Converters;

public class JsonDocumentCamelCaseConverter : JsonConverter<JsonDocument?>
{
    public override JsonDocument? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // For reading, just deserialize the JsonDocument as-is
        return JsonDocument.ParseValue(ref reader);
    }

    public override void Write(Utf8JsonWriter writer, JsonDocument? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        // Write the JSON element, recursively applying the naming policy
        WriteElement(writer, value.RootElement, options);
    }

    private static void WriteElement(Utf8JsonWriter writer, JsonElement element, JsonSerializerOptions options)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject())
                {
                    // Apply PropertyNamingPolicy to property names
                    var name = property.Name;
                    if (options.PropertyNamingPolicy != null)
                    {
                        name = options.PropertyNamingPolicy.ConvertName(property.Name);
                    }
                    writer.WritePropertyName(name);
                    WriteElement(writer, property.Value, options);
                }
                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    WriteElement(writer, item, options);
                }
                writer.WriteEndArray();
                break;

            case JsonValueKind.String:
                writer.WriteStringValue(element.GetString());
                break;

            case JsonValueKind.Number:
                if (element.TryGetInt64(out var longValue))
                {
                    writer.WriteNumberValue(longValue);
                }
                else
                {
                    writer.WriteNumberValue(element.GetDecimal());
                }
                break;

            case JsonValueKind.True:
                writer.WriteBooleanValue(true);
                break;

            case JsonValueKind.False:
                writer.WriteBooleanValue(false);
                break;

            case JsonValueKind.Null:
                writer.WriteNullValue();
                break;

            default:
                throw new NotSupportedException($"Unsupported JsonValueKind: {element.ValueKind}");
        }
    }
}
