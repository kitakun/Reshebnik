using Tabligo.Domain.Models.Integration;

namespace Tabligo.Integrations.Integrations.Ozon;

public class OzonDataTransformer
{
    public List<IntegrationImportRequest> TransformProductsToMetrics(List<OzonProduct> products)
    {
        var requests = new List<IntegrationImportRequest>();

        foreach (var product in products)
        {
            // Product metadata metric
            var properties = new Dictionary<string, object>
            {
                ["ExternalId"] = product.Id.ToString(),
                ["Unit"] = "Count",
                ["Type"] = "Metadata",
                ["PeriodType"] = "Static",
                ["OfferId"] = product.OfferId,
                ["Name"] = product.Name,
                ["Price"] = product.Price,
                ["OldPrice"] = product.OldPrice ?? "",
                ["PremiumPrice"] = product.PremiumPrice ?? "",
                ["CurrencyCode"] = product.CurrencyCode,
                ["Vat"] = product.Vat,
                ["CreatedAt"] = product.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                ["UpdatedAt"] = product.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                ["Visible"] = product.Visible,
                ["StateName"] = product.State.Name,
                ["StateId"] = product.State.StateId
            };

            requests.Add(new IntegrationImportRequest
            {
                EntityType = "Metric",
                Name = $"Product: {product.Name}",
                Description = $"Ozon product metadata for {product.Name}",
                Properties = properties,
                Confidence = 1.0m,
                Reasoning = "Imported from Ozon products",
                SourceSystem = "Ozon",
                SourceId = product.Id.ToString(),
                SourceCreatedAt = product.CreatedAt
            });
        }

        return requests;
    }

    public List<IntegrationImportRequest> TransformPostingsToMetrics(List<OzonPosting> postings)
    {
        var requests = new List<IntegrationImportRequest>();

        foreach (var posting in postings)
        {
            // Posting metadata metric
            var properties = new Dictionary<string, object>
            {
                ["ExternalId"] = posting.OrderId.ToString(),
                ["Unit"] = "Count",
                ["Type"] = "Metadata",
                ["PeriodType"] = "Static",
                ["OrderNumber"] = posting.OrderNumber,
                ["PostingNumber"] = posting.PostingNumber,
                ["Status"] = posting.Status,
                ["DeliveryMethodId"] = posting.DeliveryMethod.Id,
                ["DeliveryMethodName"] = posting.DeliveryMethod.Name,
                ["WarehouseId"] = posting.Warehouse.Id,
                ["WarehouseName"] = posting.Warehouse.Name,
                ["InPostingAt"] = posting.InPostingAt.ToString("yyyy-MM-dd HH:mm:ss"),
                ["ShipmentDate"] = posting.ShipmentDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "",
                ["DeliveringDate"] = posting.DeliveringDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "",
                ["CustomerId"] = posting.Customer.Id,
                ["CustomerName"] = posting.Customer.Name,
                ["CustomerPhone"] = posting.Customer.Phone,
                ["CustomerEmail"] = posting.Customer.Email,
                ["ProductCount"] = posting.Products.Count,
                ["TotalQuantity"] = posting.Products.Sum(p => p.Quantity)
            };

            requests.Add(new IntegrationImportRequest
            {
                EntityType = "Metric",
                Name = $"Posting: {posting.PostingNumber}",
                Description = $"Ozon posting metadata for order {posting.OrderNumber}",
                Properties = properties,
                Confidence = 1.0m,
                Reasoning = "Imported from Ozon postings",
                SourceSystem = "Ozon",
                SourceId = posting.OrderId.ToString(),
                SourceCreatedAt = posting.InPostingAt
            });

            // Sales revenue metric
            var totalRevenue = posting.FinancialData.Products.Sum(p => p.Price * p.Quantity);
            properties = new Dictionary<string, object>
            {
                ["ExternalId"] = $"{posting.OrderId}-revenue",
                ["Unit"] = "RUB",
                ["Type"] = "PlanFact",
                ["PeriodType"] = "Daily",
                ["OrderId"] = posting.OrderId,
                ["PostingNumber"] = posting.PostingNumber,
                ["Revenue"] = totalRevenue,
                ["ProductCount"] = posting.Products.Count,
                ["TotalQuantity"] = posting.Products.Sum(p => p.Quantity),
                ["CommissionAmount"] = posting.FinancialData.Products.Sum(p => p.CommissionAmount),
                ["Payout"] = posting.FinancialData.Products.Sum(p => p.Payout)
            };

            requests.Add(new IntegrationImportRequest
            {
                EntityType = "Metric",
                Name = $"Revenue - {posting.PostingNumber}",
                Description = $"Sales revenue for Ozon posting {posting.PostingNumber}",
                Properties = properties,
                Confidence = 1.0m,
                Reasoning = "Aggregated from Ozon posting financial data",
                SourceSystem = "Ozon",
                SourceId = $"{posting.OrderId}-revenue",
                SourceCreatedAt = posting.InPostingAt
            });
        }

        return requests;
    }

    public List<IntegrationImportRequest> TransformReturnsToMetrics(List<OzonReturn> returns)
    {
        var requests = new List<IntegrationImportRequest>();

        foreach (var returnItem in returns)
        {
            var properties = new Dictionary<string, object>
            {
                ["ExternalId"] = returnItem.Id.ToString(),
                ["Unit"] = "Count",
                ["Type"] = "PlanFact",
                ["PeriodType"] = "Daily",
                ["PostingNumber"] = returnItem.PostingNumber,
                ["ProductName"] = returnItem.ProductName,
                ["OfferId"] = returnItem.OfferId,
                ["Price"] = returnItem.Price,
                ["Quantity"] = returnItem.Quantity,
                ["ReturnReasonName"] = returnItem.ReturnReasonName,
                ["ReturnDate"] = returnItem.ReturnDate.ToString("yyyy-MM-dd HH:mm:ss"),
                ["ReturnStatus"] = returnItem.ReturnStatus,
                ["ReturnType"] = returnItem.ReturnType,
                ["IsOpened"] = returnItem.IsOpened,
                ["PlaceName"] = returnItem.Place.Name,
                ["PlaceId"] = returnItem.Place.Id
            };

            requests.Add(new IntegrationImportRequest
            {
                EntityType = "Metric",
                Name = $"Return: {returnItem.ProductName}",
                Description = $"Return information for {returnItem.ProductName}",
                Properties = properties,
                Confidence = 1.0m,
                Reasoning = "Imported from Ozon returns",
                SourceSystem = "Ozon",
                SourceId = returnItem.Id.ToString(),
                SourceCreatedAt = returnItem.ReturnDate
            });
        }

        return requests;
    }

    public List<IntegrationImportRequest> TransformActionsToMetrics(List<OzonAction> actions)
    {
        var requests = new List<IntegrationImportRequest>();

        foreach (var action in actions)
        {
            var properties = new Dictionary<string, object>
            {
                ["ExternalId"] = action.Id.ToString(),
                ["Unit"] = "Count",
                ["Type"] = "Metadata",
                ["PeriodType"] = "Static",
                ["Name"] = action.Name,
                ["Type"] = action.Type,
                ["StartAt"] = action.StartAt.ToString("yyyy-MM-dd HH:mm:ss"),
                ["FinishAt"] = action.FinishAt.ToString("yyyy-MM-dd HH:mm:ss"),
                ["IsEnabled"] = action.IsEnabled,
                ["Budget"] = action.Budget,
                ["CurrencyCode"] = action.CurrencyCode
            };

            requests.Add(new IntegrationImportRequest
            {
                EntityType = "Metric",
                Name = $"Action: {action.Name}",
                Description = $"Ozon promotional action: {action.Name}",
                Properties = properties,
                Confidence = 1.0m,
                Reasoning = "Imported from Ozon actions",
                SourceSystem = "Ozon",
                SourceId = action.Id.ToString(),
                SourceCreatedAt = action.StartAt
            });
        }

        return requests;
    }

    public List<IntegrationImportRequest> TransformFinancialReportsToMetrics(List<OzonFinancialReport> reports)
    {
        var requests = new List<IntegrationImportRequest>();

        foreach (var report in reports)
        {
            var properties = new Dictionary<string, object>
            {
                ["ExternalId"] = report.OperationId.ToString(),
                ["Unit"] = report.CurrencyCode,
                ["Type"] = "PlanFact",
                ["PeriodType"] = "Daily",
                ["OperationType"] = report.OperationType,
                ["OperationTypeName"] = report.OperationTypeName,
                ["OperationDate"] = report.OperationDate.ToString("yyyy-MM-dd HH:mm:ss"),
                ["DeliveryCharge"] = report.DeliveryCharge,
                ["ReturnDeliveryCharge"] = report.ReturnDeliveryCharge,
                ["AccrualsForSale"] = report.AccrualsForSale,
                ["SaleCommission"] = report.SaleCommission,
                ["Amount"] = report.Amount,
                ["Type"] = report.Type
            };

            requests.Add(new IntegrationImportRequest
            {
                EntityType = "Metric",
                Name = $"Financial: {report.OperationTypeName}",
                Description = $"Financial operation: {report.OperationTypeName}",
                Properties = properties,
                Confidence = 1.0m,
                Reasoning = "Imported from Ozon financial reports",
                SourceSystem = "Ozon",
                SourceId = report.OperationId.ToString(),
                SourceCreatedAt = report.OperationDate
            });
        }

        return requests;
    }

    public List<IntegrationImportRequest> TransformAllData(
        List<OzonProduct> products,
        List<OzonPosting> postings,
        List<OzonReturn> returns,
        List<OzonAction> actions,
        List<OzonFinancialReport> financialReports)
    {
        var allRequests = new List<IntegrationImportRequest>();

        allRequests.AddRange(TransformProductsToMetrics(products));
        allRequests.AddRange(TransformPostingsToMetrics(postings));
        allRequests.AddRange(TransformReturnsToMetrics(returns));
        allRequests.AddRange(TransformActionsToMetrics(actions));
        allRequests.AddRange(TransformFinancialReportsToMetrics(financialReports));

        return allRequests;
    }
}
