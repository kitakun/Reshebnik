using System.Text.Json;
using System.Text.Json.Serialization;
using Tabligo.Domain.Enums;

namespace Tabligo.Domain.Models.Integration;

public class GetCourseExportInfoConverter : JsonConverter<GetCourseExportInfoContainer?>
{
    public override GetCourseExportInfoContainer? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;
            
        if (reader.TokenType == JsonTokenType.StartArray)
        {
            // Read the array
            using var doc = JsonDocument.ParseValue(ref reader);
            var arrayLength = doc.RootElement.GetArrayLength();
            
            // If array has objects, it might be actual data (like groups)
            if (arrayLength > 0)
            {
                var firstElement = doc.RootElement[0];
                // If this is a group/object with id field, we're getting data directly
                if (firstElement.ValueKind == JsonValueKind.Object && firstElement.TryGetProperty("id", out var idElement))
                {
                    // This is direct data, extract it into a special structure
                    var groups = new List<object>();
                    foreach (var element in doc.RootElement.EnumerateArray())
                    {
                        var deserialized = JsonSerializer.Deserialize<object>(element.GetRawText());
                        if (deserialized != null)
                        {
                            groups.Add(deserialized);
                        }
                    }
                    // Return a container indicating we got data directly
                    return new GetCourseExportInfoContainer
                    {
                        DirectData = groups,
                        HasDirectData = true
                    };
                }
            }
            // Empty array - file not created yet
            return null;
        }
        
        // If it's an object, deserialize normally
        using var doc2 = JsonDocument.ParseValue(ref reader);
        var text = doc2.RootElement.GetRawText();
        var complexObject = JsonSerializer.Deserialize<GetCourseExportInfoContainer>(text, options);
        return complexObject;
    }

    public override void Write(Utf8JsonWriter writer, GetCourseExportInfoContainer? value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }
}

public class IntegrationTestConnectionResult
{
    [JsonPropertyName("isSuccess")]
    public bool IsSuccess { get; set; }

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    [JsonPropertyName("endpoint")]
    public string? Endpoint { get; set; }

    [JsonPropertyName("response")]
    public string? Response { get; set; }

    [JsonPropertyName("integrationType")]
    public IntegrationTypeEnum IntegrationType { get; set; }
}

public class GetCourseTestConnectionResult
{
    [JsonPropertyName("isSuccess")]
    public bool IsSuccess { get; set; }

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    [JsonPropertyName("endpoint")]
    public string? Endpoint { get; set; }

    [JsonPropertyName("response")]
    public string? Response { get; set; }
}

public class GetCourseExportFilter
{
    [JsonPropertyName("created_at_from")]
    public DateTime? CreatedAtFrom { get; set; }

    [JsonPropertyName("created_at_to")]
    public DateTime? CreatedAtTo { get; set; }

    [JsonPropertyName("user_id")]
    public int? UserId { get; set; }

    [JsonPropertyName("group_id")]
    public int? GroupId { get; set; }

    [JsonPropertyName("deal_status")]
    public string? DealStatus { get; set; }

    [JsonPropertyName("payment_status")]
    public string? PaymentStatus { get; set; }
}

public class GetCourseExportResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("info")]
    [JsonConverter(typeof(GetCourseExportInfoConverter))]
    public GetCourseExportInfoContainer? Info { get; set; }

    [JsonPropertyName("error")]
    public bool Error { get; set; }

    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }

    [JsonPropertyName("error_code")]
    public int? ErrorCode { get; set; }
}

public class GetCourseExportInfoContainer
{
    [JsonPropertyName("export_id")]
    public long? ExportId { get; set; }
    
    [JsonPropertyName("fields")]
    public string[]? Fields { get; set; }
    
    [JsonPropertyName("items")]
    public List<object[]>? Items { get; set; }
    
    [JsonPropertyName("status")]
    public string? Status { get; set; }
    
    // For handling direct data responses (like groups array)
    public List<object>? DirectData { get; set; }
    
    public bool HasDirectData { get; set; }
}


public class GetCourseUser
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    public string ExternalId { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("group_name")]
    public List<string> GroupNames { get; set; } = new();

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("last_login_at")]
    public DateTime? LastLoginAt { get; set; }
}

public class GetCourseGroup
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    public string ExternalId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

public class GetCourseOrder
{
    public long Id { get; set; }

    public string ExternalId { get; set; } = string.Empty;

    public string OrderNumber { get; set; } = string.Empty;

    public long? UserId { get; set; }

    public string? UserFullName { get; set; }

    public string? UserEmail { get; set; }

    public string? UserPhone { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? PaidAt { get; set; }

    public string Items { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public decimal TotalCost { get; set; }

    public decimal Paid { get; set; }
}

public class GetCoursePayment
{
    [JsonPropertyName("payment_id")]
    public int PaymentId { get; set; }

    public string ExternalId { get; set; } = string.Empty;

    [JsonPropertyName("deal_id")]
    public int DealId { get; set; }

    [JsonPropertyName("user_id")]
    public int UserId { get; set; }

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("payment_method")]
    public string? PaymentMethod { get; set; }

    public string Name { get; set; } = string.Empty;
}
