using Tabligo.Domain.Models.Integration;

namespace Tabligo.Integrations.Integrations.GoogleSheets;

public class GoogleSheetsDataTransformer
{
    public List<IntegrationImportRequest> TransformSpreadsheetRowsToImportRequests(
        List<List<string>> rows,
        bool hasHeaderRow = true)
    {
        var requests = new List<IntegrationImportRequest>();

        if (rows.Count == 0)
            return requests;

        // Extract headers
        var headers = hasHeaderRow && rows.Count > 0 ? rows[0] : new List<string>();
        var dataRows = hasHeaderRow && rows.Count > 1 ? rows.Skip(1).ToList() : rows;

        // Auto-detect entity type based on headers
        var entityType = DetectEntityType(headers);

        foreach (var (row, index) in dataRows.Select((r, i) => (r, i)))
        {
            if (row.All(string.IsNullOrEmpty))
                continue; // Skip empty rows

            var properties = BuildPropertiesFromRow(row, headers);

            // Try to extract a name from the first non-empty column
            var name = ExtractName(row);

            requests.Add(new IntegrationImportRequest
            {
                EntityType = entityType,
                Name = name,
                Description = $"Imported row {index + 1} from Google Sheets",
                Properties = properties,
                Confidence = 0.8m,
                Reasoning = "Auto-detected from Google Sheets data structure",
                SourceSystem = "Google Sheets",
                SourceId = $"row-{index + 1}",
                SourceCreatedAt = DateTime.Now
            });
        }

        return requests;
    }

    private string DetectEntityType(List<string> headers)
    {
        var headersLower = headers.Select(h => h.ToLowerInvariant()).ToList();

        // Check for Employee patterns
        if (headersLower.Any(h => h.Contains("name") || h.Contains("fio") || h.Contains("email")))
        {
            return "Employee";
        }

        // Check for Department patterns
        if (headersLower.Any(h => h.Contains("department") || h.Contains("team") || h.Contains("group")))
        {
            return "Department";
        }

        // Check for Metric/Indicator patterns
        if (headersLower.Any(h => h.Contains("value") || h.Contains("amount") || h.Contains("metric")))
        {
            return "Indicator";
        }

        // Default to generic data
        return "Data";
    }

    private Dictionary<string, object> BuildPropertiesFromRow(List<string> row, List<string> headers)
    {
        var properties = new Dictionary<string, object>();

        for (int i = 0; i < Math.Min(row.Count, headers.Count); i++)
        {
            var header = string.IsNullOrEmpty(headers[i]) ? $"Column{i + 1}" : headers[i];
            var value = row[i];

            // Try to parse numeric values
            if (decimal.TryParse(value, out var decimalValue))
            {
                properties[header] = decimalValue;
            }
            else if (DateTime.TryParse(value, out var dateValue))
            {
                properties[header] = dateValue.ToString("yyyy-MM-dd");
            }
            else
            {
                properties[header] = value ?? string.Empty;
            }
        }

        return properties;
    }

    private string ExtractName(List<string> row)
    {
        // Try to find a name in the first few columns
        for (int i = 0; i < Math.Min(row.Count, 3); i++)
        {
            if (!string.IsNullOrEmpty(row[i]))
                return row[i];
        }

        return $"Row {row.Count}";
    }
}


