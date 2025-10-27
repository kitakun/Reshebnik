using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Tabligo.Domain.Models.Integration;

namespace Tabligo.Integrations.Integrations.GetCourse;

public class GetCourseApiClient(HttpClient httpClient, ILogger<GetCourseApiClient> logger)
{
    private string? _lastGroupsResponse;

    public string? GetLastGroupsResponse() => _lastGroupsResponse;

    public async Task<string> ExportUsersAsync(GetCourseConfiguration config, GetCourseExportFilter? filter = null, CancellationToken ct = default)
    {
        // Step 1: Start users export
        var startUrl = $"https://{config.AccountName}.getcourse.ru/pl/api/account/users?key={config.ApiKey}";
        
        // Add filters if provided
        if (filter != null)
        {
            if (filter.CreatedAtFrom.HasValue)
                startUrl += $"&created_at[from]={filter.CreatedAtFrom.Value:yyyy-MM-dd}";
            if (filter.CreatedAtTo.HasValue)
                startUrl += $"&created_at[to]={filter.CreatedAtTo.Value:yyyy-MM-dd}";
            if (filter.UserId.HasValue)
                startUrl += $"&id={filter.UserId.Value}";
            if (filter.GroupId.HasValue)
                startUrl += $"&idgrouplist={filter.GroupId.Value}";
        }
        
        // GetCourse requires at least one filter, add default filter if none provided
        if (filter == null)
        {
            startUrl += "&created_at[from]=2014-01-01";
        }
        else if (!filter.CreatedAtFrom.HasValue && !filter.CreatedAtTo.HasValue && !filter.UserId.HasValue && !filter.GroupId.HasValue)
        {
            // Filter object exists but has no values, add default
            startUrl += "&created_at[from]=2014-01-01";
        }
        
        logger.LogInformation($"Starting GetCourse users export: {startUrl}");
        
        var request = new HttpRequestMessage(HttpMethod.Get, startUrl);
        request.Headers.Add("Accept", "application/json");
        request.Headers.Add("User-Agent", "Tabligo-Integration/1.0");
        
        var response = await httpClient.SendAsync(request, ct);
        var responseContent = await response.Content.ReadAsStringAsync(ct);
        
        logger.LogInformation($"GetCourse users export start response: {responseContent}");
        
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Failed to start users export: {response.StatusCode} - {responseContent}");
        }
        
        // Parse response to get export_id
        var exportResponse = JsonSerializer.Deserialize<GetCourseExportResponse>(responseContent);
        
        if (exportResponse?.Success != true || exportResponse.Info?.ExportId == null)
        {
            var errorMessage = exportResponse?.ErrorMessage ?? "Unknown error";
            throw new InvalidOperationException($"Users export start failed: {errorMessage}");
        }
        
        return exportResponse.Info.ExportId.Value.ToString();
    }

    public async Task<string> ExportGroupsAsync(GetCourseConfiguration config, CancellationToken ct = default)
    {
        // Step 1: Start groups export
        var startUrl = $"https://{config.AccountName}.getcourse.ru/pl/api/account/groups?key={config.ApiKey}";
        
        logger.LogInformation($"Starting GetCourse groups export: {startUrl}");
        
        var request = new HttpRequestMessage(HttpMethod.Get, startUrl);
        request.Headers.Add("Accept", "application/json");
        request.Headers.Add("User-Agent", "Tabligo-Integration/1.0");
        
        var response = await httpClient.SendAsync(request, ct);
        var responseContent = await response.Content.ReadAsStringAsync(ct);
        
        logger.LogInformation($"GetCourse groups export start response: {responseContent}");
        
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Failed to start groups export: {response.StatusCode} - {responseContent}");
        }
        
        // Store the response for potential direct data extraction
        _lastGroupsResponse = responseContent;
        
        // Parse response
        var exportResponse = JsonSerializer.Deserialize<GetCourseExportResponse>(responseContent);
        
        if (exportResponse?.Success != true)
        {
            var errorMessage = exportResponse?.ErrorMessage ?? "Unknown error";
            throw new InvalidOperationException($"Groups export start failed: {errorMessage}");
        }
        
        // Handle direct data response (groups returned directly instead of export_id)
        if (exportResponse.Info is { HasDirectData: true, DirectData: not null })
        {
            // Return a marker indicating we got data directly
            return "DIRECT_DATA";
        }
        
        if (exportResponse.Info?.ExportId == null)
        {
            throw new InvalidOperationException("Groups export failed: No export ID or direct data returned");
        }
        
        return exportResponse.Info.ExportId.Value.ToString();
    }

    public async Task<string> ExportOrdersAsync(GetCourseConfiguration config, GetCourseExportFilter? filter = null, CancellationToken ct = default)
    {
        // Step 1: Start deals export
        var startUrl = $"https://{config.AccountName}.getcourse.ru/pl/api/account/deals?key={config.ApiKey}";
        
        // Add filters if provided
        if (filter != null)
        {
            if (filter.CreatedAtFrom.HasValue)
                startUrl += $"&created_at[from]={filter.CreatedAtFrom.Value:yyyy-MM-dd}";
            if (filter.CreatedAtTo.HasValue)
                startUrl += $"&created_at[to]={filter.CreatedAtTo.Value:yyyy-MM-dd}";
            if (filter.UserId.HasValue)
                startUrl += $"&user_id={filter.UserId.Value}";
            if (!string.IsNullOrEmpty(filter.DealStatus))
                startUrl += $"&status={filter.DealStatus}";
        }
        
        // GetCourse requires at least one filter, add default filter if none provided
        if (filter == null)
        {
            startUrl += "&created_at[from]=2014-01-01";
        }
        else if (!filter.CreatedAtFrom.HasValue && !filter.CreatedAtTo.HasValue && !filter.UserId.HasValue && string.IsNullOrEmpty(filter.DealStatus))
        {
            // Filter object exists but has no values, add default
            startUrl += "&created_at[from]=2014-01-01";
        }
        
        logger.LogInformation($"Starting GetCourse deals export: {startUrl}");
        
        var request = new HttpRequestMessage(HttpMethod.Get, startUrl);
        request.Headers.Add("Accept", "application/json");
        request.Headers.Add("User-Agent", "Tabligo-Integration/1.0");
        
        var response = await httpClient.SendAsync(request, ct);
        var responseContent = await response.Content.ReadAsStringAsync(ct);
        
        logger.LogInformation($"GetCourse deals export start response: {responseContent}");
        
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Failed to start deals export: {response.StatusCode} - {responseContent}");
        }
        
        // Parse response to get export_id
        var exportResponse = JsonSerializer.Deserialize<GetCourseExportResponse>(responseContent);
        
        if (exportResponse?.Success != true || exportResponse.Info?.ExportId == null)
        {
            var errorMessage = exportResponse?.ErrorMessage ?? "Unknown error";
            throw new InvalidOperationException($"Deals export start failed: {errorMessage}");
        }
        
        return exportResponse.Info.ExportId.Value.ToString();
    }

    public async Task<string> ExportPaymentsAsync(GetCourseConfiguration config, GetCourseExportFilter? filter = null, CancellationToken ct = default)
    {
        // Step 1: Start payments export
        var startUrl = $"https://{config.AccountName}.getcourse.ru/pl/api/account/payments?key={config.ApiKey}";
        
        // Add filters if provided
        if (filter != null)
        {
            if (filter.CreatedAtFrom.HasValue)
                startUrl += $"&created_at[from]={filter.CreatedAtFrom.Value:yyyy-MM-dd}";
            if (filter.CreatedAtTo.HasValue)
                startUrl += $"&created_at[to]={filter.CreatedAtTo.Value:yyyy-MM-dd}";
            if (filter.UserId.HasValue)
                startUrl += $"&user_id={filter.UserId.Value}";
            if (!string.IsNullOrEmpty(filter.PaymentStatus))
                startUrl += $"&status={filter.PaymentStatus}";
        }
        
        // GetCourse requires at least one filter, add default filter if none provided
        if (filter == null)
        {
            startUrl += "&created_at[from]=2014-01-01";
        }
        else if (!filter.CreatedAtFrom.HasValue && !filter.CreatedAtTo.HasValue && !filter.UserId.HasValue && string.IsNullOrEmpty(filter.PaymentStatus))
        {
            // Filter object exists but has no values, add default
            startUrl += "&created_at[from]=2014-01-01";
        }
        
        logger.LogInformation($"Starting GetCourse payments export: {startUrl}");
        
        var request = new HttpRequestMessage(HttpMethod.Get, startUrl);
        request.Headers.Add("Accept", "application/json");
        request.Headers.Add("User-Agent", "Tabligo-Integration/1.0");
        
        var response = await httpClient.SendAsync(request, ct);
        var responseContent = await response.Content.ReadAsStringAsync(ct);
        
        logger.LogInformation($"GetCourse payments export start response: {responseContent}");
        
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Failed to start payments export: {response.StatusCode} - {responseContent}");
        }
        
        // Parse response to get export_id
        var exportResponse = JsonSerializer.Deserialize<GetCourseExportResponse>(responseContent);
        
        if (exportResponse?.Success != true || exportResponse.Info?.ExportId == null)
        {
            var errorMessage = exportResponse?.ErrorMessage ?? "Unknown error";
            throw new InvalidOperationException($"Payments export start failed: {errorMessage}");
        }
        
        return exportResponse.Info.ExportId.Value.ToString();
    }

    public async Task<string> DownloadExportFileAsync(GetCourseConfiguration config, string exportId, CancellationToken ct = default)
    {
        // Step 2: Poll the export until ready, then get the data
        var pollUrl = $"https://{config.AccountName}.getcourse.ru/pl/api/account/exports/{exportId}?key={config.ApiKey}";
        
        logger.LogInformation($"Polling GetCourse export {exportId}: {pollUrl}");
        
        // Poll until export is ready
        string dataJson = string.Empty;
        int maxAttempts = 30; // 30 attempts * 2 seconds = 1 minute max
        int attempt = 0;
        
        while (attempt < maxAttempts)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, pollUrl);
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("User-Agent", "Tabligo-Integration/1.0");
            
            var response = await httpClient.SendAsync(request, ct);
            dataJson = await response.Content.ReadAsStringAsync(ct);
            
            logger.LogInformation($"GetCourse export {exportId} poll attempt {attempt + 1}: {dataJson.Substring(0, Math.Min(dataJson.Length, 200))}");
            
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Failed to poll export {exportId}: {response.StatusCode} - {dataJson}");
            }
            
            // Parse response to check status with flexible options
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true
            };
            
            var exportResponse = JsonSerializer.Deserialize<GetCourseExportResponse>(dataJson, jsonOptions);
            
            // If file not yet created (error_code 909), continue polling
            if (exportResponse?.ErrorCode == 909)
            {
                logger.LogInformation($"File not yet created for export {exportId}, continuing to poll...");
            }
            // Check if export is ready - GetCourse returns data with "fields" and "items" when ready
            else if (exportResponse?.Success == true && !string.IsNullOrEmpty(dataJson) && dataJson.Contains("\"fields\"") && dataJson.Contains("\"items\""))
            {
                break;
            }
            
            // Wait 2 seconds before next attempt
            await Task.Delay(2000, ct);
            attempt++;
        }
        
        if (attempt >= maxAttempts)
        {
            throw new TimeoutException($"Export {exportId} did not complete within timeout period");
        }
        
        // Return the JSON data as a string
        return dataJson;
    }

    public async Task<string> CheckExportStatusAsync(GetCourseConfiguration config, string exportId, CancellationToken ct = default)
    {
        var url = $"https://{config.AccountName}.getcourse.ru/pl/api/account/exports/{exportId}";
        
        var formData = new List<KeyValuePair<string, string>>
        {
            new("key", config.ApiKey)
        };

        var content = new FormUrlEncodedContent(formData);
        var response = await httpClient.PostAsync(url, content, ct);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"Failed to check export status: {response.StatusCode} - {errorContent}");
        }

        var responseContent = await response.Content.ReadAsStringAsync(ct);
        var exportResponse = JsonSerializer.Deserialize<GetCourseExportResponse>(responseContent);
        
        return exportResponse?.Info?.Status ?? "unknown";
    }

    public async Task<string> DebugApiCallAsync(GetCourseConfiguration config, string action, CancellationToken ct = default)
    {
        try
        {
            var url = $"https://{config.AccountName}.getcourse.ru/pl/api/account/exports";
            
            var requestData = new Dictionary<string, string>
            {
                { "key", config.ApiKey },
                { "action", action }
            };

            var formData = new List<KeyValuePair<string, string>>();
            foreach (var kvp in requestData)
            {
                formData.Add(new KeyValuePair<string, string>(kvp.Key, kvp.Value));
            }

            var content = new FormUrlEncodedContent(formData);
            
            logger.LogInformation($"Debug GetCourse API call to: {url}");
            logger.LogInformation($"Request data: {JsonSerializer.Serialize(requestData)}");
            
            var response = await httpClient.PostAsync(url, content, ct);
            var responseContent = await response.Content.ReadAsStringAsync(ct);
            
            logger.LogInformation($"Debug GetCourse API response: {responseContent}");
            
            return responseContent;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Debug GetCourse API call failed");
            return $"Error: {ex.Message}";
        }
    }

    public async Task<GetCourseTestConnectionResult> TestConnectionAsync(GetCourseConfiguration config, CancellationToken ct = default)
    {
        try
        {
            // Test with simple users export (2-step process)
            var testResult = await TestUsersExportConnectionAsync(config, ct);
            if (testResult.IsSuccess)
            {
                return testResult;
            }
            
            // Fallback: test groups endpoint
            var groupsTest = await TestGroupsConnectionAsync(config, ct);
            if (groupsTest.IsSuccess)
            {
                return groupsTest;
            }
            
            // All tests failed
            return new GetCourseTestConnectionResult
            {
                IsSuccess = false,
                Reason = $"Все эндпоинты не удались. Экспорт пользователей: {testResult.Reason}. Группы: {groupsTest.Reason}",
                Endpoint = "Тестирование нескольких эндпоинтов"
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetCourse API connection test failed");
            return new GetCourseTestConnectionResult
            {
                IsSuccess = false,
                Reason = $"Тест соединения не удался с исключением: {ex.Message}",
                Endpoint = "Неизвестно"
            };
        }
    }

    private async Task<GetCourseTestConnectionResult> TestUsersExportConnectionAsync(GetCourseConfiguration config, CancellationToken ct)
    {
        try
        {
            // Step 1: Start users export
            var startUrl = $"https://{config.AccountName}.getcourse.ru/pl/api/account/users?key={config.ApiKey}&created_at[from]={DateTime.UtcNow.AddDays(-1):yyyy-MM-dd}";
            
            logger.LogInformation($"Testing GetCourse Users Export API connection to: {startUrl}");
            
            var request = new HttpRequestMessage(HttpMethod.Get, startUrl);
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("User-Agent", "Tabligo-Integration/1.0");
            
            var response = await httpClient.SendAsync(request, ct);
            var responseContent = await response.Content.ReadAsStringAsync(ct);
            
            logger.LogInformation($"GetCourse Users Export API test response: {responseContent}");
            
            // Check if we got HTML instead of JSON
            if (responseContent.TrimStart().StartsWith("<"))
            {
                return new GetCourseTestConnectionResult
                {
                    IsSuccess = false,
                    Reason = $"API экспорта пользователей вернул HTML вместо JSON. Возможно, неправильный URL или эндпоинт недоступен. Ответ: {responseContent.Substring(0, Math.Min(200, responseContent.Length))}...",
                    Endpoint = startUrl,
                    Response = responseContent
                };
            }
            
            if (response.IsSuccessStatusCode)
            {
                // Check if response contains export_id (successful export start)
                var isSuccess = responseContent.Contains("\"success\":true") && responseContent.Contains("\"export_id\"");
                return new GetCourseTestConnectionResult
                {
                    IsSuccess = isSuccess,
                    Reason = isSuccess ? "Тест соединения с API экспорта пользователей успешен" : $"API экспорта пользователей вернул ошибку: {responseContent}",
                    Endpoint = startUrl,
                    Response = responseContent
                };
            }
            
            return new GetCourseTestConnectionResult
            {
                IsSuccess = false,
                Reason = $"API экспорта пользователей вернул HTTP {response.StatusCode}: {responseContent}",
                Endpoint = startUrl,
                Response = responseContent
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "GetCourse Users Export API test failed");
            return new GetCourseTestConnectionResult
            {
                IsSuccess = false,
                Reason = $"Тест API экспорта пользователей не удался с исключением: {ex.Message}",
                Endpoint = $"https://{config.AccountName}.getcourse.ru/pl/api/account/users"
            };
        }
    }

    private async Task<GetCourseTestConnectionResult> TestGroupsConnectionAsync(GetCourseConfiguration config, CancellationToken ct)
    {
        try
        {
            // Test groups endpoint
            var groupsUrl = $"https://{config.AccountName}.getcourse.ru/pl/api/account/groups?key={config.ApiKey}";
            
            logger.LogInformation($"Testing GetCourse Groups API connection to: {groupsUrl}");
            
            var request = new HttpRequestMessage(HttpMethod.Get, groupsUrl);
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("User-Agent", "Tabligo-Integration/1.0");
            
            var response = await httpClient.SendAsync(request, ct);
            var responseContent = await response.Content.ReadAsStringAsync(ct);
            
            logger.LogInformation($"GetCourse Groups API test response: {responseContent}");
            
            // Check if we got HTML instead of JSON
            if (responseContent.TrimStart().StartsWith("<"))
            {
                return new GetCourseTestConnectionResult
                {
                    IsSuccess = false,
                    Reason = $"API групп вернул HTML вместо JSON. Возможно, неправильный URL или эндпоинт недоступен. Ответ: {responseContent.Substring(0, Math.Min(200, responseContent.Length))}...",
                    Endpoint = groupsUrl,
                    Response = responseContent
                };
            }
            
            if (response.IsSuccessStatusCode)
            {
                // Check if response contains success indicator
                var isSuccess = responseContent.Contains("\"success\":true") || !responseContent.Contains("\"error\":true");
                return new GetCourseTestConnectionResult
                {
                    IsSuccess = isSuccess,
                    Reason = isSuccess ? "Тест соединения с API групп успешен" : $"API групп вернул ошибку: {responseContent}",
                    Endpoint = groupsUrl,
                    Response = responseContent
                };
            }
            
            return new GetCourseTestConnectionResult
            {
                IsSuccess = false,
                Reason = $"API групп вернул HTTP {response.StatusCode}: {responseContent}",
                Endpoint = groupsUrl,
                Response = responseContent
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "GetCourse Groups API test failed");
            return new GetCourseTestConnectionResult
            {
                IsSuccess = false,
                Reason = $"Тест API групп не удался с исключением: {ex.Message}",
                Endpoint = $"https://{config.AccountName}.getcourse.ru/pl/api/account/groups"
            };
        }
    }

    private async Task<GetCourseTestConnectionResult> TestAccountInfoConnectionAsync(GetCourseConfiguration config, CancellationToken ct)
    {
        var url = $"https://{config.AccountName}.getcourse.ru/pl/api/account/info";
        try
        {
            var formData = new List<KeyValuePair<string, string>>
            {
                new("key", config.ApiKey)
            };

            var content = new FormUrlEncodedContent(formData);
            
            // Add proper headers for API request
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("User-Agent", "Tabligo-Integration/1.0");
            
            logger.LogInformation($"Testing GetCourse Account Info API connection to: {url}");
            logger.LogInformation($"Request data: key={config.ApiKey}");
            
            var response = await httpClient.SendAsync(request, ct);
            var responseContent = await response.Content.ReadAsStringAsync(ct);
            
            logger.LogInformation($"GetCourse Account Info API test response: {responseContent}");
            
            // Check if we got HTML instead of JSON (common issue)
            if (responseContent.TrimStart().StartsWith("<"))
            {
                return new GetCourseTestConnectionResult
                {
                    IsSuccess = false,
                    Reason = $"API вернул HTML вместо JSON. Возможно, неправильный URL или эндпоинт недоступен. Ответ: {responseContent.Substring(0, Math.Min(200, responseContent.Length))}...",
                    Endpoint = url,
                    Response = responseContent
                };
            }
            
            if (response.IsSuccessStatusCode)
            {
                // Check if response contains success indicator
                var isSuccess = responseContent.Contains("\"success\":true") || !responseContent.Contains("\"error\":true");
                return new GetCourseTestConnectionResult
                {
                    IsSuccess = isSuccess,
                    Reason = isSuccess ? "Тест соединения с API информации об аккаунте успешен" : $"API информации об аккаунте вернул ошибку: {responseContent}",
                    Endpoint = url,
                    Response = responseContent
                };
            }
            
            return new GetCourseTestConnectionResult
            {
                IsSuccess = false,
                Reason = $"API информации об аккаунте вернул HTTP {response.StatusCode}: {responseContent}",
                Endpoint = url,
                Response = responseContent
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "GetCourse Account Info API test failed");
            return new GetCourseTestConnectionResult
            {
                IsSuccess = false,
                Reason = $"Тест API информации об аккаунте не удался с исключением: {ex.Message}",
                Endpoint = url
            };
        }
    }

    private async Task<GetCourseTestConnectionResult> TestExportConnectionAsync(GetCourseConfiguration config, CancellationToken ct)
    {
        try
        {
            // Test connection by making a simple export request with minimal parameters
            var url = $"https://{config.AccountName}.getcourse.ru/pl/api/account/exports";
            
            // Create minimal parameters for testing
            var testParams = new Dictionary<string, object>();
            
            var paramsJson = JsonSerializer.Serialize(testParams);
            var paramsBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(paramsJson));
            
            var formData = new List<KeyValuePair<string, string>>
            {
                new("key", config.ApiKey),
                new("action", "export_users"),
                new("params", paramsBase64)
            };

            var content = new FormUrlEncodedContent(formData);
            
            // Add proper headers for API request
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("User-Agent", "Tabligo-Integration/1.0");
            
            logger.LogInformation($"Testing GetCourse Export API connection to: {url}");
            logger.LogInformation($"Request data: key={config.ApiKey}, action=export_users, params={paramsBase64}");
            logger.LogInformation($"Base64 decoded params: {paramsJson}");
            
            var response = await httpClient.SendAsync(request, ct);
            var responseContent = await response.Content.ReadAsStringAsync(ct);
            
            logger.LogInformation($"GetCourse Export API test response: {responseContent}");
            
            // Check if we got HTML instead of JSON (common issue)
            if (responseContent.TrimStart().StartsWith("<"))
            {
                return new GetCourseTestConnectionResult
                {
                    IsSuccess = false,
                    Reason = $"API экспорта вернул HTML вместо JSON. Возможно, неправильный URL или эндпоинт недоступен. Ответ: {responseContent.Substring(0, Math.Min(200, responseContent.Length))}...",
                    Endpoint = url,
                    Response = responseContent
                };
            }
            
            if (response.IsSuccessStatusCode)
            {
                // Check if response contains success indicator
                var isSuccess = responseContent.Contains("\"success\":true") || !responseContent.Contains("\"error\":true");
                return new GetCourseTestConnectionResult
                {
                    IsSuccess = isSuccess,
                    Reason = isSuccess ? "Тест соединения с API экспорта успешен" : $"API экспорта вернул ошибку: {responseContent}",
                    Endpoint = url,
                    Response = responseContent
                };
            }
            
            return new GetCourseTestConnectionResult
            {
                IsSuccess = false,
                Reason = $"API экспорта вернул HTTP {response.StatusCode}: {responseContent}",
                Endpoint = url,
                Response = responseContent
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "GetCourse Export API test failed, trying user management endpoint");
            return new GetCourseTestConnectionResult
            {
                IsSuccess = false,
                Reason = $"Тест API экспорта не удался с исключением: {ex.Message}",
                Endpoint = $"https://{config.AccountName}.getcourse.ru/pl/api/account/exports"
            };
        }
    }

    private async Task<GetCourseTestConnectionResult> TestUserManagementConnectionAsync(GetCourseConfiguration config, CancellationToken ct)
    {
        try
        {
            // Test connection using user management endpoint
            var url = $"https://{config.AccountName}.getcourse.ru/pl/api/users";
            
            // Create a minimal test user data
            var testUserData = new Dictionary<string, object>
            {
                { "user", new Dictionary<string, object>
                    {
                        { "email", $"test-{Guid.NewGuid()}@example.com" },
                        { "first_name", "Test" },
                        { "last_name", "User" }
                    }
                },
                { "system", new Dictionary<string, object>
                    {
                        { "refresh_if_exists", 0 }
                    }
                }
            };
            
            var paramsJson = JsonSerializer.Serialize(testUserData);
            var paramsBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(paramsJson));
            
            var formData = new List<KeyValuePair<string, string>>
            {
                new("key", config.ApiKey),
                new("action", "add"),
                new("params", paramsBase64)
            };

            var content = new FormUrlEncodedContent(formData);
            
            // Add proper headers for API request
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("User-Agent", "Tabligo-Integration/1.0");
            
            logger.LogInformation($"Testing GetCourse User Management API connection to: {url}");
            
            var response = await httpClient.SendAsync(request, ct);
            var responseContent = await response.Content.ReadAsStringAsync(ct);
            
            logger.LogInformation($"GetCourse User Management API test response: {responseContent}");
            
            if (response.IsSuccessStatusCode)
            {
                // Check if response contains success indicator
                var isSuccess = responseContent.Contains("\"success\":true") && !responseContent.Contains("\"success\":false");
                return new GetCourseTestConnectionResult
                {
                    IsSuccess = isSuccess,
                    Reason = isSuccess ? "Тест соединения с API управления пользователями успешен" : $"API управления пользователями вернул ошибку: {responseContent}",
                    Endpoint = url,
                    Response = responseContent
                };
            }
            
            return new GetCourseTestConnectionResult
            {
                IsSuccess = false,
                Reason = $"API управления пользователями вернул HTTP {response.StatusCode}: {responseContent}",
                Endpoint = url,
                Response = responseContent
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetCourse User Management API test failed");
            return new GetCourseTestConnectionResult
            {
                IsSuccess = false,
                Reason = $"Тест API управления пользователями не удался с исключением: {ex.Message}",
                Endpoint = $"https://{config.AccountName}.getcourse.ru/pl/api/users"
            };
        }
    }

    private async Task<string> CreateExportAsync(GetCourseConfiguration config, string action, Dictionary<string, object> parameters, CancellationToken ct)
    {
        // According to GetCourse API documentation, export endpoint is different
        var url = $"https://{config.AccountName}.getcourse.ru/pl/api/account/exports";
        
        // Build parameters as JSON string
        var paramsJson = JsonSerializer.Serialize(parameters);
        
        // According to documentation: "Параметры для действия передаются в формате JSON, закодированного в base64 в параметре params POST-запроса"
        var paramsBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(paramsJson));
        
        var formData = new List<KeyValuePair<string, string>>
        {
            new("key", config.ApiKey),
            new("action", action),
            new("params", paramsBase64)
        };

        var content = new FormUrlEncodedContent(formData);
        
        logger.LogInformation($"Making GetCourse API request to: {url}");
        logger.LogInformation($"Action: {action}");
        logger.LogInformation($"Parameters JSON: {paramsJson}");
        logger.LogInformation($"Parameters Base64: {paramsBase64}");
        
        var response = await httpClient.PostAsync(url, content, ct);
        
        var responseContent = await response.Content.ReadAsStringAsync(ct);
        logger.LogInformation($"GetCourse API response: {responseContent}");
        
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Failed to create export: {response.StatusCode} - {responseContent}");
        }

        var exportResponse = JsonSerializer.Deserialize<GetCourseExportResponse>(responseContent);
        
        if (exportResponse?.Success != true || exportResponse.Error)
        {
            var errorMessage = exportResponse?.ErrorMessage ?? "Unknown error";
            throw new InvalidOperationException($"Export creation failed: {errorMessage}");
        }

        if (exportResponse.Info?.ExportId == null)
        {
            throw new InvalidOperationException("Export ID not returned from GetCourse API");
        }

        return exportResponse.Info.ExportId.Value.ToString();
    }
}
