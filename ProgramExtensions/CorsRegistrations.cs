namespace Tabligo.Web.ProgramExtensions;

public static class CorsRegistrations
{
    public static IServiceCollection AddTabligoCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("DevCors", policy =>
            {
#if DEBUG
                policy
                    .WithOrigins("http://localhost:5173")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
                policy
                    .WithOrigins("https://tabligo.ru")
                    .AllowAnyHeader()
                    .AllowAnyMethod();
                policy
                    .WithOrigins("https://new.tabligo.ru")
                    .AllowAnyHeader()
                    .AllowAnyMethod();
#else
                policy
                    .WithOrigins("https://tabligo.ru")
                    .AllowAnyHeader()
                    .AllowAnyMethod();
                policy
                    .WithOrigins("https://new.tabligo.ru")
                    .AllowAnyHeader()
                    .AllowAnyMethod();
#endif
            });
        });

        return services;
    }
}
