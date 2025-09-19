using Microsoft.OpenApi.Models;

namespace Reshebnik.Web.ProgramExtensions;

public static class SwaggerConfigurations
{
    public static IServiceCollection AddReshebnikSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("Client", new OpenApiInfo { Title = "Client API", Version = "v1" });
            options.SwaggerDoc("Super", new OpenApiInfo { Title = "Super API", Version = "v1" });

            // Optional: remove default grouping by controller name
            options.TagActionsBy(api =>
            {
                var groupName = api.GroupName;
                return [groupName ?? api.ActionDescriptor.RouteValues["controller"]];
            });
            options.DocInclusionPredicate((docName, apiDesc) =>
            {
                var groupName = apiDesc.GroupName;
                return groupName == docName;
            });

            options.AddSecurityDefinition("Bearer", new()
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter: Bearer {your JWT token}"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new() { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    new List<string>()
                }
            });
        });

        return services;
    }
}
