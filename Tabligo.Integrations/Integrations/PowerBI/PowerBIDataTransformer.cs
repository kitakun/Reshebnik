using Tabligo.Domain.Models.Integration;

namespace Tabligo.Integrations.Integrations.PowerBI;

public class PowerBIDataTransformer
{
    public List<IntegrationImportRequest> TransformWorkspacesToDepartments(List<PowerBIWorkspace> workspaces)
    {
        var requests = new List<IntegrationImportRequest>();

        foreach (var workspace in workspaces)
        {
            var properties = new Dictionary<string, object>
            {
                ["ExternalId"] = workspace.Id,
                ["Description"] = workspace.Description ?? "",
                ["Type"] = workspace.Type,
                ["State"] = workspace.State,
                ["IsOnDedicatedCapacity"] = workspace.IsOnDedicatedCapacity,
                ["CapacityId"] = workspace.CapacityId ?? "",
                ["DefaultDatasetStorageFormat"] = workspace.DefaultDatasetStorageFormat ?? "",
                ["CreatedDate"] = workspace.CreatedDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "",
                ["UserCount"] = workspace.Users.Count
            };

            requests.Add(new IntegrationImportRequest
            {
                EntityType = "Department",
                Name = workspace.Name,
                Description = workspace.Description ?? $"PowerBI workspace: {workspace.Name}",
                Properties = properties,
                Confidence = 1.0m,
                Reasoning = "Imported from PowerBI workspaces",
                SourceSystem = "PowerBI",
                SourceId = workspace.Id,
                SourceCreatedAt = workspace.CreatedDate
            });
        }

        return requests;
    }

    public List<IntegrationImportRequest> TransformDatasetsToMetrics(List<PowerBIDataset> datasets)
    {
        var requests = new List<IntegrationImportRequest>();

        foreach (var dataset in datasets)
        {
            // Create dataset metadata metric
            var properties = new Dictionary<string, object>
            {
                ["ExternalId"] = dataset.Id,
                ["Unit"] = "Count",
                ["Type"] = "Metadata",
                ["PeriodType"] = "Static",
                ["Description"] = dataset.Description ?? "",
                ["WebUrl"] = dataset.WebUrl ?? "",
                ["IsRefreshable"] = dataset.IsRefreshable,
                ["IsEffectiveIdentityRequired"] = dataset.IsEffectiveIdentityRequired,
                ["IsOnPremGatewayRequired"] = dataset.IsOnPremGatewayRequired,
                ["TargetStorageMode"] = dataset.TargetStorageMode ?? "",
                ["CreatedDate"] = dataset.CreatedDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "",
                ["ConfiguredBy"] = dataset.ConfiguredBy ?? "",
                ["TableCount"] = dataset.Tables.Count,
                ["AddRowsAPIEnabled"] = dataset.AddRowsAPIEnabled
            };

            requests.Add(new IntegrationImportRequest
            {
                EntityType = "Metric",
                Name = $"Dataset: {dataset.Name}",
                Description = $"PowerBI dataset metadata for {dataset.Name}",
                Properties = properties,
                Confidence = 1.0m,
                Reasoning = "Imported from PowerBI datasets",
                SourceSystem = "PowerBI",
                SourceId = dataset.Id,
                SourceCreatedAt = dataset.CreatedDate
            });

            // Create table count metric for each dataset
            properties = new Dictionary<string, object>
            {
                ["ExternalId"] = $"{dataset.Id}-table-count",
                ["Unit"] = "Count",
                ["Type"] = "PlanFact",
                ["PeriodType"] = "Static",
                ["DatasetId"] = dataset.Id,
                ["DatasetName"] = dataset.Name,
                ["TableCount"] = dataset.Tables.Count
            };

            requests.Add(new IntegrationImportRequest
            {
                EntityType = "Metric",
                Name = $"Table Count - {dataset.Name}",
                Description = $"Number of tables in PowerBI dataset {dataset.Name}",
                Properties = properties,
                Confidence = 1.0m,
                Reasoning = "Aggregated from PowerBI dataset tables",
                SourceSystem = "PowerBI",
                SourceId = $"{dataset.Id}-table-count",
                SourceCreatedAt = dataset.CreatedDate
            });
        }

        return requests;
    }

    public List<IntegrationImportRequest> TransformReportsToMetrics(List<PowerBIReport> reports)
    {
        var requests = new List<IntegrationImportRequest>();

        foreach (var report in reports)
        {
            var properties = new Dictionary<string, object>
            {
                ["ExternalId"] = report.Id,
                ["Unit"] = "Count",
                ["Type"] = "Metadata",
                ["PeriodType"] = "Static",
                ["Description"] = report.Description ?? "",
                ["WebUrl"] = report.WebUrl ?? "",
                ["EmbedUrl"] = report.EmbedUrl ?? "",
                ["DatasetId"] = report.DatasetId ?? "",
                ["ReportType"] = report.ReportType,
                ["CreatedDate"] = report.CreatedDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "",
                ["ModifiedDate"] = report.ModifiedDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "",
                ["CreatedBy"] = report.CreatedBy ?? "",
                ["ModifiedBy"] = report.ModifiedBy ?? "",
                ["IsOwnedByMe"] = report.IsOwnedByMe,
                ["IsPublished"] = report.IsPublished
            };

            requests.Add(new IntegrationImportRequest
            {
                EntityType = "Metric",
                Name = $"Report: {report.Name}",
                Description = $"PowerBI report metadata for {report.Name}",
                Properties = properties,
                Confidence = 1.0m,
                Reasoning = "Imported from PowerBI reports",
                SourceSystem = "PowerBI",
                SourceId = report.Id,
                SourceCreatedAt = report.CreatedDate
            });
        }

        return requests;
    }

    public List<IntegrationImportRequest> TransformDashboardsToMetrics(List<PowerBIDashboard> dashboards)
    {
        var requests = new List<IntegrationImportRequest>();

        foreach (var dashboard in dashboards)
        {
            var properties = new Dictionary<string, object>
            {
                ["ExternalId"] = dashboard.Id,
                ["Unit"] = "Count",
                ["Type"] = "Metadata",
                ["PeriodType"] = "Static",
                ["Description"] = dashboard.Description ?? "",
                ["WebUrl"] = dashboard.WebUrl ?? "",
                ["EmbedUrl"] = dashboard.EmbedUrl ?? "",
                ["IsReadOnly"] = dashboard.IsReadOnly,
                ["TileCount"] = dashboard.Tiles.Count
            };

            requests.Add(new IntegrationImportRequest
            {
                EntityType = "Metric",
                Name = $"Dashboard: {dashboard.DisplayName}",
                Description = $"PowerBI dashboard metadata for {dashboard.DisplayName}",
                Properties = properties,
                Confidence = 1.0m,
                Reasoning = "Imported from PowerBI dashboards",
                SourceSystem = "PowerBI",
                SourceId = dashboard.Id,
                SourceCreatedAt = null // Dashboards don't have created date in API
            });

            // Create tile count metric for each dashboard
            properties = new Dictionary<string, object>
            {
                ["ExternalId"] = $"{dashboard.Id}-tile-count",
                ["Unit"] = "Count",
                ["Type"] = "PlanFact",
                ["PeriodType"] = "Static",
                ["DashboardId"] = dashboard.Id,
                ["DashboardName"] = dashboard.DisplayName,
                ["TileCount"] = dashboard.Tiles.Count
            };

            requests.Add(new IntegrationImportRequest
            {
                EntityType = "Metric",
                Name = $"Tile Count - {dashboard.DisplayName}",
                Description = $"Number of tiles in PowerBI dashboard {dashboard.DisplayName}",
                Properties = properties,
                Confidence = 1.0m,
                Reasoning = "Aggregated from PowerBI dashboard tiles",
                SourceSystem = "PowerBI",
                SourceId = $"{dashboard.Id}-tile-count",
                SourceCreatedAt = null
            });
        }

        return requests;
    }

    public List<IntegrationImportRequest> TransformUsersToEmployees(List<PowerBIUser> users)
    {
        var requests = new List<IntegrationImportRequest>();

        foreach (var user in users)
        {
            var fio = user.DisplayName;
            if (string.IsNullOrEmpty(fio))
                fio = user.EmailAddress?.Split('@')[0] ?? user.Identifier;

            var properties = new Dictionary<string, object>
            {
                ["ExternalId"] = user.Identifier,
                ["Email"] = user.EmailAddress ?? "",
                ["FIO"] = fio,
                ["JobTitle"] = "PowerBI User", // Default for PowerBI users
                ["DefaultRole"] = "Employee",
                ["GraphId"] = user.GraphId ?? "",
                ["PrincipalType"] = user.PrincipalType,
                ["UserType"] = user.UserType ?? ""
            };

            requests.Add(new IntegrationImportRequest
            {
                EntityType = "Employee",
                Name = fio,
                Description = $"PowerBI user: {user.EmailAddress ?? user.Identifier}",
                Properties = properties,
                Confidence = 1.0m,
                Reasoning = "Imported from PowerBI users",
                SourceSystem = "PowerBI",
                SourceId = user.Identifier,
                SourceCreatedAt = null // Users don't have created date in API
            });
        }

        return requests;
    }

    public List<IntegrationImportRequest> TransformAllData(
        List<PowerBIWorkspace> workspaces,
        List<PowerBIDataset> datasets,
        List<PowerBIReport> reports,
        List<PowerBIDashboard> dashboards,
        List<PowerBIUser> users)
    {
        var allRequests = new List<IntegrationImportRequest>();

        allRequests.AddRange(TransformWorkspacesToDepartments(workspaces));
        allRequests.AddRange(TransformDatasetsToMetrics(datasets));
        allRequests.AddRange(TransformReportsToMetrics(reports));
        allRequests.AddRange(TransformDashboardsToMetrics(dashboards));
        allRequests.AddRange(TransformUsersToEmployees(users));

        return allRequests;
    }
}
