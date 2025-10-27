using System.Text.Json.Serialization;

namespace Tabligo.Domain.Models.Integration;

public class PowerBIAccessTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("scope")]
    public string Scope { get; set; } = string.Empty;
}

public class PowerBIWorkspace
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("isOnDedicatedCapacity")]
    public bool IsOnDedicatedCapacity { get; set; }

    [JsonPropertyName("capacityId")]
    public string? CapacityId { get; set; }

    [JsonPropertyName("defaultDatasetStorageFormat")]
    public string? DefaultDatasetStorageFormat { get; set; }

    [JsonPropertyName("createdDate")]
    public DateTime? CreatedDate { get; set; }

    [JsonPropertyName("users")]
    public List<PowerBIWorkspaceUser> Users { get; set; } = new();
}

public class PowerBIWorkspaceUser
{
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; } = string.Empty;

    [JsonPropertyName("principalType")]
    public string PrincipalType { get; set; } = string.Empty;

    [JsonPropertyName("groupUserAccessRight")]
    public string GroupUserAccessRight { get; set; } = string.Empty;

    [JsonPropertyName("emailAddress")]
    public string? EmailAddress { get; set; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }
}

public class PowerBIDataset
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("webUrl")]
    public string? WebUrl { get; set; }

    [JsonPropertyName("isRefreshable")]
    public bool IsRefreshable { get; set; }

    [JsonPropertyName("isEffectiveIdentityRequired")]
    public bool IsEffectiveIdentityRequired { get; set; }

    [JsonPropertyName("isEffectiveIdentityRolesRequired")]
    public bool IsEffectiveIdentityRolesRequired { get; set; }

    [JsonPropertyName("isOnPremGatewayRequired")]
    public bool IsOnPremGatewayRequired { get; set; }

    [JsonPropertyName("targetStorageMode")]
    public string? TargetStorageMode { get; set; }

    [JsonPropertyName("createdDate")]
    public DateTime? CreatedDate { get; set; }

    [JsonPropertyName("createReportEmbedURL")]
    public string? CreateReportEmbedURL { get; set; }

    [JsonPropertyName("qnaEmbedURL")]
    public string? QnaEmbedURL { get; set; }

    [JsonPropertyName("addRowsAPIEnabled")]
    public bool AddRowsAPIEnabled { get; set; }

    [JsonPropertyName("configuredBy")]
    public string? ConfiguredBy { get; set; }

    [JsonPropertyName("isDefaultDataMart")]
    public bool IsDefaultDataMart { get; set; }

    [JsonPropertyName("tables")]
    public List<PowerBITable> Tables { get; set; } = new();
}

public class PowerBITable
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("columns")]
    public List<PowerBIColumn> Columns { get; set; } = new();

    [JsonPropertyName("rows")]
    public List<Dictionary<string, object>> Rows { get; set; } = new();
}

public class PowerBIColumn
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("dataType")]
    public string DataType { get; set; } = string.Empty;
}

public class PowerBIReport
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("webUrl")]
    public string? WebUrl { get; set; }

    [JsonPropertyName("embedUrl")]
    public string? EmbedUrl { get; set; }

    [JsonPropertyName("datasetId")]
    public string? DatasetId { get; set; }

    [JsonPropertyName("reportType")]
    public string ReportType { get; set; } = string.Empty;

    [JsonPropertyName("createdDate")]
    public DateTime? CreatedDate { get; set; }

    [JsonPropertyName("modifiedDate")]
    public DateTime? ModifiedDate { get; set; }

    [JsonPropertyName("createdBy")]
    public string? CreatedBy { get; set; }

    [JsonPropertyName("modifiedBy")]
    public string? ModifiedBy { get; set; }

    [JsonPropertyName("isOwnedByMe")]
    public bool IsOwnedByMe { get; set; }

    [JsonPropertyName("isPublished")]
    public bool IsPublished { get; set; }
}

public class PowerBIDashboard
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("webUrl")]
    public string? WebUrl { get; set; }

    [JsonPropertyName("embedUrl")]
    public string? EmbedUrl { get; set; }

    [JsonPropertyName("isReadOnly")]
    public bool IsReadOnly { get; set; }

    [JsonPropertyName("tiles")]
    public List<PowerBITile> Tiles { get; set; } = new();
}

public class PowerBITile
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("subTitle")]
    public string? SubTitle { get; set; }

    [JsonPropertyName("embedUrl")]
    public string? EmbedUrl { get; set; }

    [JsonPropertyName("embedData")]
    public string? EmbedData { get; set; }

    [JsonPropertyName("rowSpan")]
    public int RowSpan { get; set; }

    [JsonPropertyName("colSpan")]
    public int ColSpan { get; set; }
}

public class PowerBIUser
{
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("emailAddress")]
    public string? EmailAddress { get; set; }

    [JsonPropertyName("graphId")]
    public string? GraphId { get; set; }

    [JsonPropertyName("principalType")]
    public string PrincipalType { get; set; } = string.Empty;

    [JsonPropertyName("userType")]
    public string? UserType { get; set; }
}
