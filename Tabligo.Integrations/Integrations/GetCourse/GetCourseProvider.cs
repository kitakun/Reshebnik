using System.Text.Json;
using Microsoft.Extensions.Logging;
using Tabligo.Domain.Enums;
using Tabligo.Domain.Models.Integration;
using Tabligo.Domain.Models.JobOperation;

namespace Tabligo.Integrations.Integrations.GetCourse;

public class GetCourseProvider(
    GetCourseApiClient apiClient,
    GetCourseDataTransformer transformer,
    ILogger<GetCourseProvider> logger)
    : IIntegrationProvider
{
    public IntegrationTypeEnum Type => IntegrationTypeEnum.GetCourse;

    public async Task<object?> ProcessAsync(
        Domain.Entities.JobOperationEntity job,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        if (job.InputData == null)
        {
            throw new InvalidOperationException("Job does not contain input data");
        }

        // Deserialize input data
        var inputData = JsonSerializer.Deserialize<IntegrationSyncInputData>(
            job.InputData.RootElement.GetRawText(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (inputData == null)
        {
            throw new InvalidOperationException("Failed to deserialize input data");
        }

        // Fetch data using FetchDataAsync
        var importRequests = await FetchDataAsync(
            inputData.CompanyId,
            inputData.Configuration,
            inputData.Filters,
            cancellationToken);

        return importRequests;
    }

    private class IntegrationSyncInputData
    {
        public int CompanyId { get; set; }
        public string Configuration { get; set; } = string.Empty;
        public Dictionary<string, object>? Filters { get; set; }
    }

    public async Task<bool> TestConnectionAsync(string configuration, CancellationToken ct)
    {
        var result = await TestConnectionDetailedAsync(configuration, ct);
        return result.IsSuccess;
    }

    public async Task<IntegrationTestConnectionResult> TestConnectionDetailedAsync(string configuration, CancellationToken ct)
    {
        try
        {
            var config = JsonSerializer.Deserialize<GetCourseConfiguration>(configuration);
            if (config == null || string.IsNullOrEmpty(config.AccountName) || string.IsNullOrEmpty(config.ApiKey))
            {
                return new IntegrationTestConnectionResult
                {
                    IsSuccess = false,
                    Reason = "Неверная конфигурация: отсутствует имя аккаунта или API ключ",
                    Endpoint = "Проверка конфигурации",
                    IntegrationType = IntegrationTypeEnum.GetCourse
                };
            }

            var result = await apiClient.TestConnectionAsync(config, ct);
            
            // Update configuration with validation result
            config.IsValid = result.IsSuccess;
            
            return new IntegrationTestConnectionResult
            {
                IsSuccess = result.IsSuccess,
                Reason = result.Reason,
                Endpoint = result.Endpoint,
                Response = result.Response,
                IntegrationType = IntegrationTypeEnum.GetCourse
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetCourse connection test failed");
            return new IntegrationTestConnectionResult
            {
                IsSuccess = false,
                Reason = $"Тест соединения не удался с исключением: {ex.Message}",
                Endpoint = "Обработка исключений",
                IntegrationType = IntegrationTypeEnum.GetCourse
            };
        }
    }

    public async Task<bool> ActivateAsync(int companyId, string configuration, CancellationToken ct)
    {
        try
        {
            var config = JsonSerializer.Deserialize<GetCourseConfiguration>(configuration);
            if (config == null || string.IsNullOrEmpty(config.AccountName) || string.IsNullOrEmpty(config.ApiKey))
            {
                return false;
            }

            // Test connection to ensure configuration is valid
            var result = await apiClient.TestConnectionAsync(config, ct);
            return result.IsSuccess;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetCourse activation failed");
            return false;
        }
    }

    public Task<bool> DeactivateAsync(int companyId, CancellationToken ct)
    {
        // GetCourse doesn't require any cleanup for deactivation
        return Task.FromResult(true);
    }

    public async Task<List<IntegrationImportRequest>> FetchDataAsync(
        int companyId, 
        string configuration,
        Dictionary<string, object>? filters = null,
        CancellationToken ct = default)
    {
        try
        {
            // Extract filters from configuration if not provided
            if (filters == null)
            {
                filters = ExtractFiltersFromConfiguration(configuration);
            }

            var config = JsonSerializer.Deserialize<GetCourseConfiguration>(configuration);
            if (config == null || string.IsNullOrEmpty(config.AccountName) || string.IsNullOrEmpty(config.ApiKey))
            {
                throw new InvalidOperationException("Invalid GetCourse configuration");
            }

            // Extract and convert filters
            GetCourseExportFilter? exportFilter = null;
            if (filters != null)
            {
                exportFilter = new GetCourseExportFilter();
                
                if (filters.TryGetValue("dateFrom", out var dateFrom) && dateFrom != null)
                {
                    if (DateTime.TryParse(dateFrom.ToString(), out var fromDate))
                        exportFilter.CreatedAtFrom = fromDate;
                }
                
                if (filters.TryGetValue("dateTo", out var dateTo) && dateTo != null)
                {
                    if (DateTime.TryParse(dateTo.ToString(), out var toDate))
                        exportFilter.CreatedAtTo = toDate;
                }
            }

            var allRequests = new List<IntegrationImportRequest>();

            // Export and transform users
            if (config.ImportUsers)
            {
                logger.LogInformation("Exporting GetCourse users for company {CompanyId}", companyId);
                var usersExportId = await apiClient.ExportUsersAsync(config, exportFilter, ct);
                var usersJson = await apiClient.DownloadExportFileAsync(config, usersExportId, ct);
                var users = await ParseUsersFromJsonAsync(usersJson);
                allRequests.AddRange(transformer.TransformUsersToEmployees(users));
            }

            // Export and transform groups
            if (config.ImportGroups)
            {
                logger.LogInformation("Exporting GetCourse groups for company {CompanyId}", companyId);
                var groupsExportId = await apiClient.ExportGroupsAsync(config, ct);
                
                // Check if we got data directly
                if (groupsExportId == "DIRECT_DATA")
                {
                    // Data was returned directly, parse it from the response
                    var groupsJson = apiClient.GetLastGroupsResponse();
                    if (string.IsNullOrEmpty(groupsJson))
                    {
                        logger.LogWarning("No groups response stored for direct data");
                    }
                    else
                    {
                        var groups = await ParseGroupsFromJsonAsync(groupsJson);
                        allRequests.AddRange(transformer.TransformGroupsToDepartments(groups));
                    }
                }
                else
                {
                    // Normal async export flow
                    var groupsJson = await apiClient.DownloadExportFileAsync(config, groupsExportId, ct);
                    var groups = await ParseGroupsFromJsonAsync(groupsJson);
                    allRequests.AddRange(transformer.TransformGroupsToDepartments(groups));
                }
            }

            // Export and transform orders
            if (config.ImportOrders)
            {
                logger.LogInformation("Exporting GetCourse orders for company {CompanyId}", companyId);
                var ordersExportId = await apiClient.ExportOrdersAsync(config, exportFilter, ct);
                var ordersJson = await apiClient.DownloadExportFileAsync(config, ordersExportId, ct);
                var orders = await ParseOrdersFromJsonAsync(ordersJson);
                allRequests.AddRange(transformer.TransformOrdersToMetrics(orders));
            }

            // Export and transform payments
            if (config.ImportPayments)
            {
                logger.LogInformation("Exporting GetCourse payments for company {CompanyId}", companyId);
                var paymentsExportId = await apiClient.ExportPaymentsAsync(config, exportFilter, ct);
                var paymentsJson = await apiClient.DownloadExportFileAsync(config, paymentsExportId, ct);
                var payments = await ParsePaymentsFromJsonAsync(paymentsJson);
                allRequests.AddRange(transformer.TransformPaymentsToMetrics(payments));
            }

            logger.LogInformation("Successfully fetched {Count} items from GetCourse for company {CompanyId}", 
                allRequests.Count, companyId);

            return allRequests;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch data from GetCourse for company {CompanyId}", companyId);
            throw;
        }
    }

    private Task<List<GetCourseUser>> ParseUsersFromJsonAsync(string jsonContent)
    {
        // Parse JSON from GetCourse API response
        var users = new List<GetCourseUser>();
        
        var document = JsonDocument.Parse(jsonContent);
        var root = document.RootElement;
        
        if (!root.TryGetProperty("success", out var successProp) || successProp.GetBoolean() == false)
        {
            logger.LogWarning("Failed to parse GetCourse users export response");
            return Task.FromResult(users);
        }

        // GetCourse returns data in info.fields and info.items
        var fields = new List<string>();
        var items = new List<List<string>>();

        if (root.TryGetProperty("info", out var infoElement))
        {
            if (infoElement.TryGetProperty("fields", out var fieldsElement))
            {
                fields = JsonSerializer.Deserialize<List<string>>(fieldsElement.GetRawText()) ?? new List<string>();
            }

            if (infoElement.TryGetProperty("items", out var itemsElement) && itemsElement.ValueKind == JsonValueKind.Array)
            {
                // Manually parse items to handle nested arrays - extract values immediately
                foreach (var itemArray in itemsElement.EnumerateArray())
                {
                    if (itemArray.ValueKind == JsonValueKind.Array)
                    {
                        var item = new List<string>();
                        foreach (var element in itemArray.EnumerateArray())
                        {
                            // Extract the raw text immediately to avoid JsonElement lifetime issues
                            string value = element.ValueKind switch
                            {
                                JsonValueKind.Array => element.GetRawText(),
                                JsonValueKind.Object => element.GetRawText(),
                                _ => element.GetString() ?? element.ToString()
                            };
                            item.Add(value);
                        }
                        items.Add(item);
                    }
                }
            }
        }

        foreach (var item in items)
        {
            if (item.Count == 0) continue;

            var user = new GetCourseUser();
            string? registrationType = null;
            
            for (int i = 0; i < Math.Min(fields.Count, item.Count); i++)
            {
                var field = fields[i];
                string value = item[i];

                switch (field)
                {
                    case "id":
                        user.ExternalId = value;
                        if (int.TryParse(value, out int id)) user.Id = id;
                        break;
                    case "Email":
                        user.Email = value;
                        break;
                    case "Тип регистрации":
                        registrationType = value;
                        break;
                    case "Телефон":
                        user.Phone = value;
                        break;
                    case "Имя":
                        user.FirstName = value;
                        break;
                    case "Фамилия":
                        user.LastName = value;
                        break;
                    case "Город":
                        user.City = value;
                        break;
                    case "Страна":
                        user.Country = value;
                        break;
                    case "Создан":
                        if (DateTime.TryParse(value, out DateTime createdAt))
                            user.CreatedAt = createdAt;
                        break;
                    case "Последняя активность":
                        if (DateTime.TryParse(value, out DateTime lastLoginAt))
                            user.LastLoginAt = lastLoginAt;
                        break;
                }
            }
            
            // Only add users who registered independently
            if (registrationType == "Приглашен администратором")
            {
                users.Add(user);
            }
        }

        return Task.FromResult(users);
    }

    private Task<List<GetCourseGroup>> ParseGroupsFromJsonAsync(string jsonContent)
    {
        // Parse JSON from GetCourse API response
        var groups = new List<GetCourseGroup>();
        
        var document = JsonDocument.Parse(jsonContent);
        var root = document.RootElement;
        
        if (!root.TryGetProperty("success", out var successProp) || successProp.GetBoolean() == false)
        {
            logger.LogWarning("Failed to parse GetCourse groups export response");
            return Task.FromResult(groups);
        }

        // GetCourse returns data in info.fields and info.items
        var fields = new List<string>();
        var items = new List<List<string>>(); // Changed to JsonElement to handle nested arrays

        if (root.TryGetProperty("info", out var infoElement))
        {
            // Handle direct array format: {"success":true,"info":[{"id":4512019,"name":"...","last_added_at":null}]}
            if (infoElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var groupElement in infoElement.EnumerateArray())
                {
                    var group = new GetCourseGroup();
                    
                    if (groupElement.TryGetProperty("id", out var idElement))
                    {
                        // Handle both string and number IDs
                        if (idElement.ValueKind == JsonValueKind.String)
                        {
                            var idStr = idElement.GetString();
                            if (!string.IsNullOrEmpty(idStr))
                            {
                                group.ExternalId = idStr;
                                if (int.TryParse(idStr, out int id))
                                    group.Id = id;
                            }
                        }
                        else if (idElement.ValueKind == JsonValueKind.Number)
                        {
                            var id = idElement.GetInt32();
                            group.Id = id;
                            group.ExternalId = id.ToString();
                        }
                    }
                    
                    if (groupElement.TryGetProperty("name", out var nameElement))
                    {
                        group.Name = nameElement.GetString() ?? string.Empty;
                    }
                    
                    if (groupElement.TryGetProperty("last_added_at", out var lastAddedAtElement))
                    {
                        if (lastAddedAtElement.ValueKind != JsonValueKind.Null)
                        {
                            if (lastAddedAtElement.ValueKind == JsonValueKind.String)
                            {
                                var dateStr = lastAddedAtElement.GetString();
                                if (!string.IsNullOrEmpty(dateStr) && DateTime.TryParse(dateStr, out DateTime createdAt))
                                {
                                    group.CreatedAt = createdAt;
                                }
                                else
                                {
                                    group.CreatedAt = DateTime.UtcNow;
                                }
                            }
                            else
                            {
                                group.CreatedAt = DateTime.UtcNow;
                            }
                        }
                        else
                        {
                            group.CreatedAt = DateTime.UtcNow;
                        }
                    }
                    else
                    {
                        group.CreatedAt = DateTime.UtcNow;
                    }
                    
                    groups.Add(group);
                }
                
                return Task.FromResult(groups);
            }
            
            // Handle export format with fields and items (original format)
            if (infoElement.TryGetProperty("fields", out var fieldsElement))
            {
                fields = JsonSerializer.Deserialize<List<string>>(fieldsElement.GetRawText()) ?? new List<string>();
            }

            if (infoElement.TryGetProperty("items", out var itemsElement) && itemsElement.ValueKind == JsonValueKind.Array)
            {
                // Manually parse items to handle nested arrays - extract values immediately
                foreach (var itemArray in itemsElement.EnumerateArray())
                {
                    if (itemArray.ValueKind == JsonValueKind.Array)
                    {
                        var item = new List<string>();
                        foreach (var element in itemArray.EnumerateArray())
                        {
                            // Extract the raw text immediately to avoid JsonElement lifetime issues
                            string value = element.ValueKind switch
                            {
                                JsonValueKind.Array => element.GetRawText(),
                                JsonValueKind.Object => element.GetRawText(),
                                _ => element.GetString() ?? element.ToString()
                            };
                            item.Add(value);
                        }
                        items.Add(item);
                    }
                }
            }
        }

        foreach (var item in items)
        {
            if (item.Count == 0) continue;

            var group = new GetCourseGroup();
            
            for (int i = 0; i < Math.Min(fields.Count, item.Count); i++)
            {
                var field = fields[i];
                string value = item[i];

                switch (field)
                {
                    case "id":
                        group.ExternalId = value;
                        if (int.TryParse(value, out int id)) group.Id = id;
                        break;
                    case "Название":
                    case "Name":
                        group.Name = value;
                        break;
                    case "Описание":
                    case "Description":
                        group.Description = value;
                        break;
                    case "Создан":
                    case "Created_at":
                        if (DateTime.TryParse(value, out DateTime createdAt))
                            group.CreatedAt = createdAt;
                        break;
                }
            }
            groups.Add(group);
        }

        return Task.FromResult(groups);
    }

    private Task<List<GetCourseOrder>> ParseOrdersFromJsonAsync(string jsonContent)
    {
        // Parse JSON from GetCourse API response
        var orders = new List<GetCourseOrder>();
        
        var document = JsonDocument.Parse(jsonContent);
        var root = document.RootElement;
        
        if (!root.TryGetProperty("success", out var successProp) || successProp.GetBoolean() == false)
        {
            logger.LogWarning("Failed to parse GetCourse orders export response");
            return Task.FromResult(orders);
        }

        // GetCourse returns data in info.fields and info.items
        var fields = new List<string>();
        var items = new List<List<string>>();

        if (root.TryGetProperty("info", out var infoElement))
        {
            if (infoElement.TryGetProperty("fields", out var fieldsElement))
            {
                fields = JsonSerializer.Deserialize<List<string>>(fieldsElement.GetRawText()) ?? new List<string>();
            }

            if (infoElement.TryGetProperty("items", out var itemsElement) && itemsElement.ValueKind == JsonValueKind.Array)
            {
                // Manually parse items to handle nested arrays - extract values immediately
                foreach (var itemArray in itemsElement.EnumerateArray())
                {
                    if (itemArray.ValueKind == JsonValueKind.Array)
                    {
                        var item = new List<string>();
                        foreach (var element in itemArray.EnumerateArray())
                        {
                            // Extract the raw text immediately to avoid JsonElement lifetime issues
                            string value = element.ValueKind switch
                            {
                                JsonValueKind.Array => element.GetRawText(),
                                JsonValueKind.Object => element.GetRawText(),
                                _ => element.GetString() ?? element.ToString()
                            };
                            item.Add(value);
                        }
                        items.Add(item);
                    }
                }
            }
        }

        foreach (var item in items)
        {
            if (item.Count == 0) continue;

            var order = new GetCourseOrder();
            
            for (int i = 0; i < Math.Min(fields.Count, item.Count); i++)
            {
                var field = fields[i];
                string value = item[i];

                switch (field)
                {
                    case "ID заказа":
                        order.ExternalId = value;
                        if (long.TryParse(value, out long id)) order.Id = id;
                        break;
                    case "Номер":
                        order.OrderNumber = value;
                        break;
                    case "ID пользователя":
                        if (long.TryParse(value, out long userId)) order.UserId = userId;
                        break;
                    case "Пользователь":
                        order.UserFullName = value;
                        break;
                    case "Email":
                        order.UserEmail = value;
                        break;
                    case "Телефон":
                        order.UserPhone = value;
                        break;
                    case "Дата создания":
                        if (DateTime.TryParse(value, out DateTime createdAt))
                            order.CreatedAt = createdAt;
                        break;
                    case "Дата оплаты":
                        if (DateTime.TryParse(value, out DateTime paidAt))
                            order.PaidAt = paidAt;
                        break;
                    case "Состав заказа":
                        order.Items = value;
                        break;
                    case "Статус":
                        order.Status = value;
                        break;
                    case "Стоимость, RUB":
                        if (decimal.TryParse(value, out decimal totalCost))
                            order.TotalCost = totalCost;
                        break;
                    case "Оплачено":
                        if (decimal.TryParse(value, out decimal paid))
                            order.Paid = paid;
                        break;
                }
            }
            orders.Add(order);
        }

        return Task.FromResult(orders);
    }

    private Task<List<GetCoursePayment>> ParsePaymentsFromJsonAsync(string jsonContent)
    {
        // Parse JSON from GetCourse API response
        var payments = new List<GetCoursePayment>();
        
        var document = JsonDocument.Parse(jsonContent);
        var root = document.RootElement;
        
        if (!root.TryGetProperty("success", out var successProp) || successProp.GetBoolean() == false)
        {
            logger.LogWarning("Failed to parse GetCourse payments export response");
            return Task.FromResult(payments);
        }

        // GetCourse returns data in info.fields and info.items
        var fields = new List<string>();
        var items = new List<List<string>>();

        if (root.TryGetProperty("info", out var infoElement))
        {
            if (infoElement.TryGetProperty("fields", out var fieldsElement))
            {
                fields = JsonSerializer.Deserialize<List<string>>(fieldsElement.GetRawText()) ?? new List<string>();
            }

            if (infoElement.TryGetProperty("items", out var itemsElement) && itemsElement.ValueKind == JsonValueKind.Array)
            {
                // Manually parse items to handle nested arrays - extract values immediately
                foreach (var itemArray in itemsElement.EnumerateArray())
                {
                    if (itemArray.ValueKind == JsonValueKind.Array)
                    {
                        var item = new List<string>();
                        foreach (var element in itemArray.EnumerateArray())
                        {
                            // Extract the raw text immediately to avoid JsonElement lifetime issues
                            string value = element.ValueKind switch
                            {
                                JsonValueKind.Array => element.GetRawText(),
                                JsonValueKind.Object => element.GetRawText(),
                                _ => element.GetString() ?? element.ToString()
                            };
                            item.Add(value);
                        }
                        items.Add(item);
                    }
                }
            }
        }

        foreach (var item in items)
        {
            if (item.Count == 0) continue;

            var payment = new GetCoursePayment();
            
            for (int i = 0; i < Math.Min(fields.Count, item.Count); i++)
            {
                var field = fields[i];
                string value = item[i];

                switch (field)
                {
                    case "Номер":
                    case "ID платежа":
                    case "Payment Id":
                        payment.ExternalId = value;
                        if (int.TryParse(value, out int paymentId)) payment.PaymentId = paymentId;
                        break;
                    case "Заказ":
                    case "ID заказа":
                    case "ID сделки":
                    case "Deal ID":
                        if (int.TryParse(value, out int dealId)) payment.DealId = dealId;
                        break;
                    case "Пользователь":
                    case "ID пользователя":
                    case "User ID":
                        // Note: This is just the user name, not the ID
                        break;
                    case "Сумма":
                    case "Amount":
                        // Extract numeric value from strings like "300 руб." or "100 руб."
                        var numericPart = System.Text.RegularExpressions.Regex.Match(value, @"[\d.]+").Value;
                        if (decimal.TryParse(numericPart, out decimal amount))
                            payment.Amount = amount;
                        break;
                    case "Статус":
                    case "Status":
                        payment.Status = value;
                        break;
                    case "Дата создания":
                    case "Created At":
                    case "Date":
                        if (DateTime.TryParse(value, out DateTime createdAt))
                            payment.CreatedAt = createdAt;
                        break;
                    case "Тип":
                    case "Способ оплаты":
                    case "Payment Method":
                        payment.PaymentMethod = value;
                        break;
                    case "Название":
                    case "Name":
                    case "Title":
                        payment.Name = value;
                        break;
                }
            }
            payments.Add(payment);
        }

        return Task.FromResult(payments);
    }

    private Dictionary<string, object>? ExtractFiltersFromConfiguration(string configuration)
    {
        try
        {
            var requestDict = JsonSerializer.Deserialize<Dictionary<string, object>>(configuration);
            if (requestDict != null && requestDict.TryGetValue("filters", out var filtersValue))
            {
                if (filtersValue is JsonElement filtersElement)
                {
                    return JsonSerializer.Deserialize<Dictionary<string, object>>(filtersElement.GetRawText());
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to extract filters from GetCourse configuration");
        }
        
        return null;
    }
}