using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Reshebnik.SberGPT.Models;
using Reshebnik.SberGPT.Services;

namespace Reshebnik.SberGPT.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSberGpt(this IServiceCollection services, Action<SberGptOptions> configureOptions)
    {
        services.Configure(configureOptions);
        
        // Register both HTTP and gRPC services, but use gRPC as default
        services.AddHttpClient<SberGptService>();
        // services.AddScoped<ISberGptService, SberGptGrpcService>();
        services.AddScoped<ISberGptService, SberGptService>();
        return services;
    }

    public static IServiceCollection AddSberGpt(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SberGptOptions>(configuration.GetSection("SberGpt"));
        
        // Register both HTTP and gRPC services, but use gRPC as default
        services.AddHttpClient<SberGptService>();
        // services.AddScoped<ISberGptService, SberGptGrpcService>();
        services.AddScoped<ISberGptService, SberGptService>();
        
        return services;
    }
}
