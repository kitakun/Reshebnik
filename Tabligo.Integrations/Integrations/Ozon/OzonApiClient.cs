using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Logging;
using Tabligo.Domain.Models.Integration;

namespace Tabligo.Integrations.Integrations.Ozon;

public class OzonApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OzonApiClient> _logger;
    private const string BaseUrl = "https://api-seller.ozon.ru";

    public OzonApiClient(HttpClient httpClient, ILogger<OzonApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> TestConnectionAsync(OzonConfiguration config, CancellationToken ct = default)
    {
        try
        {
            // Test connection by fetching product list with limit 1
            var request = new
            {
                filter = new { visibility = "ALL" },
                last_id = "",
                limit = 1
            };

            var response = await PostAsync<OzonProductListResponse>(
                config, "/v3/product/list", request, ct);
            
            return response != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ozon connection test failed");
            return false;
        }
    }

    public async Task<List<OzonProduct>> GetProductsAsync(OzonConfiguration config, CancellationToken ct = default)
    {
        var allProducts = new List<OzonProduct>();
        string? lastId = null;

        do
        {
            var request = new
            {
                filter = new { visibility = "ALL" },
                last_id = lastId ?? "",
                limit = Math.Min(config.Limit, 1000)
            };

            var response = await PostAsync<OzonProductListResponse>(
                config, "/v3/product/list", request, ct);

            if (response?.Items != null)
            {
                allProducts.AddRange(response.Items);
                lastId = response.LastId;
            }
            else
            {
                break;
            }

        } while (!string.IsNullOrEmpty(lastId));

        return allProducts;
    }

    public async Task<List<OzonPosting>> GetPostingsAsync(OzonConfiguration config, CancellationToken ct = default)
    {
        var allPostings = new List<OzonPosting>();
        string? nextToken = null;

        do
        {
            object request;
            if (!string.IsNullOrEmpty(nextToken))
            {
                request = new
                {
                    filter = new
                    {
                        since = config.DateFrom?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                        to = config.DateTo?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                    },
                    limit = Math.Min(config.Limit, 1000),
                    with = new { analytics_data = true, financial_data = true },
                    sort_dir = "ASC",
                    offset = nextToken
                };
            }
            else
            {
                request = new
                {
                    filter = new
                    {
                        since = config.DateFrom?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                        to = config.DateTo?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                    },
                    limit = Math.Min(config.Limit, 1000),
                    with = new { analytics_data = true, financial_data = true },
                    sort_dir = "ASC"
                };
            }

            var response = await PostAsync<OzonPostingListResponse>(
                config, "/v3/posting/fbs/list", request, ct);

            if (response?.Result != null)
            {
                allPostings.AddRange(response.Result);
                nextToken = response.NextToken;
            }
            else
            {
                break;
            }

        } while (!string.IsNullOrEmpty(nextToken));

        return allPostings;
    }

    public async Task<List<OzonReturn>> GetReturnsAsync(OzonConfiguration config, CancellationToken ct = default)
    {
        var allReturns = new List<OzonReturn>();
        string? nextToken = null;

        do
        {
            object request;
            if (!string.IsNullOrEmpty(nextToken))
            {
                request = new
                {
                    filter = new
                    {
                        since = config.DateFrom?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                        to = config.DateTo?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                    },
                    limit = Math.Min(config.Limit, 1000),
                    sort_dir = "ASC",
                    offset = nextToken
                };
            }
            else
            {
                request = new
                {
                    filter = new
                    {
                        since = config.DateFrom?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                        to = config.DateTo?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                    },
                    limit = Math.Min(config.Limit, 1000),
                    sort_dir = "ASC"
                };
            }

            var response = await PostAsync<OzonReturnListResponse>(
                config, "/v1/returns/list", request, ct);

            if (response?.Result != null)
            {
                allReturns.AddRange(response.Result);
                nextToken = response.NextToken;
            }
            else
            {
                break;
            }

        } while (!string.IsNullOrEmpty(nextToken));

        return allReturns;
    }

    public async Task<List<OzonAction>> GetActionsAsync(OzonConfiguration config, CancellationToken ct = default)
    {
        try
        {
            var response = await GetAsync<OzonActionListResponse>(
                config, "/v1/actions", ct);
            
            return response?.Actions ?? new List<OzonAction>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch Ozon actions");
            return new List<OzonAction>();
        }
    }

    public async Task<List<OzonActionProduct>> GetActionProductsAsync(OzonConfiguration config, long actionId, CancellationToken ct = default)
    {
        try
        {
            var request = new { action_id = actionId };
            var response = await PostAsync<OzonActionProductListResponse>(
                config, "/v1/actions/products", request, ct);
            
            return response?.Products ?? new List<OzonActionProduct>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch Ozon action products for action {ActionId}", actionId);
            return new List<OzonActionProduct>();
        }
    }

    public async Task<List<OzonFinancialReport>> GetFinancialReportsAsync(OzonConfiguration config, CancellationToken ct = default)
    {
        var allReports = new List<OzonFinancialReport>();
        string? nextToken = null;

        do
        {
            object request;
            if (!string.IsNullOrEmpty(nextToken))
            {
                request = new
                {
                    filter = new
                    {
                        date = new
                        {
                            from = config.DateFrom?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                            to = config.DateTo?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                        }
                    },
                    limit = Math.Min(config.Limit, 1000),
                    sort_dir = "ASC",
                    offset = nextToken
                };
            }
            else
            {
                request = new
                {
                    filter = new
                    {
                        date = new
                        {
                            from = config.DateFrom?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                            to = config.DateTo?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                        }
                    },
                    limit = Math.Min(config.Limit, 1000),
                    sort_dir = "ASC"
                };
            }

            var response = await PostAsync<OzonFinancialReportListResponse>(
                config, "/v1/finance/cash-flow-statement/list", request, ct);

            if (response?.Result != null)
            {
                allReports.AddRange(response.Result);
                nextToken = response.NextToken;
            }
            else
            {
                break;
            }

        } while (!string.IsNullOrEmpty(nextToken));

        return allReports;
    }

    private async Task<T?> PostAsync<T>(OzonConfiguration config, string endpoint, object request, CancellationToken ct)
    {
        var url = $"{BaseUrl}{endpoint}";
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content
        };

        httpRequest.Headers.Add("Client-Id", config.ClientId);
        httpRequest.Headers.Add("Api-Key", config.ApiKey);

        var response = await _httpClient.SendAsync(httpRequest, ct);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<T>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    private async Task<T?> GetAsync<T>(OzonConfiguration config, string endpoint, CancellationToken ct)
    {
        var url = $"{BaseUrl}{endpoint}";
        var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);

        httpRequest.Headers.Add("Client-Id", config.ClientId);
        httpRequest.Headers.Add("Api-Key", config.ApiKey);

        var response = await _httpClient.SendAsync(httpRequest, ct);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<T>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }
}
