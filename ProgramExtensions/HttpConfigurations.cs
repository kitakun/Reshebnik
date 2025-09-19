using System.Text.Json;
using System.Text.Json.Serialization;
using Reshebnik.Web.Converters;

namespace Reshebnik.Web.ProgramExtensions;

public static class HttpConfigurations
{
    public static IServiceCollection AddReshebnikHttpConfigurations(this IServiceCollection services)
    {
        services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
            options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.JsonSerializerOptions.Converters.Add(new DateTimeUtcConverter());
            options.JsonSerializerOptions.Converters.Add(new NullableDateTimeUtcConverter());
        });

        services.ConfigureHttpJsonOptions(_ => { });
        services.AddResponseCaching();

        return services;
    }
}
