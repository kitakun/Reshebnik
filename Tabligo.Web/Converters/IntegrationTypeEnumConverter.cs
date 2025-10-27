using System.Text.Json;
using System.Text.Json.Serialization;
using Tabligo.Domain.Enums;

namespace Tabligo.Web.Converters;

public class IntegrationTypeEnumConverter : JsonConverter<IntegrationTypeEnum>
{
    public override IntegrationTypeEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString();
            if (string.IsNullOrEmpty(stringValue))
                throw new JsonException($"Cannot convert null or empty string to {nameof(IntegrationTypeEnum)}");

            // Convert string to enum (case-insensitive)
            if (Enum.TryParse<IntegrationTypeEnum>(stringValue, ignoreCase: true, out var enumValue))
            {
                return enumValue;
            }

            throw new JsonException($"Cannot convert '{stringValue}' to {nameof(IntegrationTypeEnum)}");
        }

        throw new JsonException($"Unexpected token type: {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, IntegrationTypeEnum value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString().ToLowerInvariant());
    }
}
