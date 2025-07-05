using Reshebnik.Domain.Models;

namespace Reshebnik.Clickhouse.Handlers;

public class FetchUserMetricsHandler
{
    public record MetricsDataResponse(int[] PlanData, int[] FactData);
    
    public async Task<MetricsDataResponse> HandleAsync(DateRange range, string key, CancellationToken cancellationToken)
    {
        // TODO implement clickhouse fetch
        return new MetricsDataResponse(new int[13], new int[13]);
    }
}