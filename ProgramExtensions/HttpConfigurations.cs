using System.Text.Json;
using System.Text.Json.Serialization;

using Tabligo.Web.Converters;

namespace Tabligo.Web.ProgramExtensions;

public static class HttpConfigurations
{
    public static IServiceCollection AddTabligoHttpConfigurations(this IServiceCollection services)
    {
        services.AddControllers(options =>
        {
            options.ModelBinderProviders.Insert(0, new IntegrationTypeEnumModelBinderProvider());
        }).AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
            options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.JsonSerializerOptions.Converters.Add(new DateTimeUtcConverter());
            options.JsonSerializerOptions.Converters.Add(new NullableDateTimeUtcConverter());
            options.JsonSerializerOptions.Converters.Add(new IntegrationTypeEnumConverter());
            options.JsonSerializerOptions.Converters.Add(new JsonDocumentCamelCaseConverter());
        });

        services.ConfigureHttpJsonOptions(_ => { });
        services.AddResponseCaching();

        return services;
    }
}
