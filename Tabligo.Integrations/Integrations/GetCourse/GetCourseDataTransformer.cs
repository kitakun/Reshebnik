using Tabligo.Domain.Models.Integration;

namespace Tabligo.Integrations.Integrations.GetCourse;

public class GetCourseDataTransformer
{
    public List<IntegrationImportRequest> TransformUsersToEmployees(List<GetCourseUser> users)
    {
        var requests = new List<IntegrationImportRequest>();

        foreach (var user in users)
        {
            var fio = $"{user.FirstName} {user.LastName}".Trim();
            if (string.IsNullOrEmpty(fio))
                fio = user.Email.Split('@')[0];

            var properties = new Dictionary<string, object>
            {
                ["ExternalId"] = user.Id.ToString(),
                ["Email"] = user.Email,
                ["Phone"] = user.Phone ?? "",
                ["FIO"] = fio,
                ["JobTitle"] = "Студент", // По умолчанию для пользователей GetCourse
                ["DefaultRole"] = "Сотрудник",
                ["City"] = user.City ?? "",
                ["Country"] = user.Country ?? "",
                ["GroupNames"] = user.GroupNames,
                ["LastLoginAt"] = user.LastLoginAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? ""
            };

            requests.Add(new IntegrationImportRequest
            {
                EntityType = "Employee",
                Name = fio,
                Description = $"Пользователь GetCourse: {user.Email}",
                Properties = properties,
                Confidence = 1.0m,
                Reasoning = "Импортировано из пользователей GetCourse",
                SourceSystem = "GetCourse",
                SourceId = user.Id.ToString(),
                SourceCreatedAt = user.CreatedAt,
                FrontendTag = "users"
            });
        }

        return requests;
    }

    public List<IntegrationImportRequest> TransformGroupsToDepartments(List<GetCourseGroup> groups)
    {
        var requests = new List<IntegrationImportRequest>();

        foreach (var group in groups)
        {
            var properties = new Dictionary<string, object>
            {
                ["ExternalId"] = group.Id.ToString(),
                ["Description"] = group.Description ?? ""
            };

            requests.Add(new IntegrationImportRequest
            {
                EntityType = "Department",
                Name = group.Name,
                Description = group.Description ?? $"Группа GetCourse: {group.Name}",
                Properties = properties,
                Confidence = 1.0m,
                Reasoning = "Импортировано из групп GetCourse",
                SourceSystem = "GetCourse",
                SourceId = group.Id.ToString(),
                SourceCreatedAt = group.CreatedAt,
                FrontendTag = "groups"
            });
        }

        return requests;
    }

    public List<IntegrationImportRequest> TransformOrdersToMetrics(List<GetCourseOrder> orders)
    {
        var requests = new List<IntegrationImportRequest>();

        if (orders.Count == 0)
            return requests;

        // Создаем единую метрику со всеми заказами для построения графиков
        var orderData = orders.Select(o => new Dictionary<string, object>
        {
            ["ExternalId"] = o.ExternalId,
            ["Date"] = o.CreatedAt.ToString("yyyy-MM-dd"),
            ["Year"] = o.CreatedAt.Year,
            ["Month"] = o.CreatedAt.Month,
            ["Status"] = o.Status,
            ["TotalCost"] = o.TotalCost,
            ["Paid"] = o.Paid
        }).ToList();

        var totalOrders = orders.Count;
        var totalRevenue = orders.Sum(o => o.TotalCost);
        var totalPaid = orders.Sum(o => o.Paid);
        var avgOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;

        var properties = new Dictionary<string, object>
        {
            ["ExternalId"] = "orders-overview",
            ["Unit"] = "Количество",
            ["Type"] = "Fact",
            ["TotalOrderCount"] = totalOrders,
            ["TotalRevenue"] = totalRevenue,
            ["TotalPaid"] = totalPaid,
            ["AverageOrderValue"] = avgOrderValue,
            ["Orders"] = orderData
        };

        requests.Add(new IntegrationImportRequest
        {
            EntityType = "Indicator",
            Name = "Заказы GetCourse",
            Description = "Все заказы GetCourse с деталями",
            Properties = properties,
            Confidence = 1.0m,
            Reasoning = "Агрегировано из заказов GetCourse",
            SourceSystem = "GetCourse",
            SourceId = "orders-overview",
            SourceCreatedAt = orders.Max(o => o.CreatedAt),
            FrontendTag = "orders"
        });

        return requests;
    }

    public List<IntegrationImportRequest> TransformPaymentsToMetrics(List<GetCoursePayment> payments)
    {
        var requests = new List<IntegrationImportRequest>();

        if (payments.Count == 0)
            return requests;

        // Создаем единую метрику со всеми платежами для построения графиков
        var paymentData = payments.Select(p => new Dictionary<string, object>
        {
            ["ExternalId"] = p.ExternalId,
            ["Date"] = p.CreatedAt.ToString("yyyy-MM-dd"),
            ["Year"] = p.CreatedAt.Year,
            ["Month"] = p.CreatedAt.Month,
            ["Status"] = p.Status,
            ["Amount"] = p.Amount
        }).ToList();

        var totalPayments = payments.Count;
        var totalAmount = payments.Sum(p => p.Amount);
        var avgAmount = totalPayments > 0 ? totalAmount / totalPayments : 0;

        var properties = new Dictionary<string, object>
        {
            ["ExternalId"] = "payments-overview",
            ["Unit"] = "Количество",
            ["Type"] = "Fact",
            ["TotalPaymentCount"] = totalPayments,
            ["TotalAmount"] = totalAmount,
            ["AverageAmount"] = avgAmount,
            ["Payments"] = paymentData
        };

        requests.Add(new IntegrationImportRequest
        {
            EntityType = "Indicator",
            Name = "Платежи GetCourse",
            Description = "Все платежи GetCourse с деталями",
            Properties = properties,
            Confidence = 1.0m,
            Reasoning = "Агрегировано из платежей GetCourse",
            SourceSystem = "GetCourse",
            SourceId = "payments-overview",
            SourceCreatedAt = payments.Max(p => p.CreatedAt),
            FrontendTag = "payments"
        });

        return requests;
    }

    public List<IntegrationImportRequest> TransformAllData(
        List<GetCourseUser> users,
        List<GetCourseGroup> groups,
        List<GetCourseOrder> orders,
        List<GetCoursePayment> payments)
    {
        var allRequests = new List<IntegrationImportRequest>();

        allRequests.AddRange(TransformUsersToEmployees(users));
        allRequests.AddRange(TransformGroupsToDepartments(groups));
        allRequests.AddRange(TransformOrdersToMetrics(orders));
        allRequests.AddRange(TransformPaymentsToMetrics(payments));

        return allRequests;
    }
}
