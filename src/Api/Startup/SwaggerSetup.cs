using FastEndpoints.Swagger;

namespace Spine.Api.Startup;

/// <summary>Swagger document and UI, Development only.</summary>
public static class SwaggerSetup
{
    public static IServiceCollection AddSpineSwagger(this IServiceCollection services, IWebHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            services.SwaggerDocument(o =>
            {
                o.DocumentSettings = s =>
                {
                    s.Title = "Spine API";
                    s.Version = "v1";
                };
            });
        }

        return services;
    }

    public static WebApplication UseSpineSwagger(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwaggerGen();
        }

        return app;
    }
}
