using Serilog;
using Serilog.Formatting.Compact;

namespace Spine.Api.Startup;

/// <summary>Serilog from configuration: plain console in Development, compact JSON elsewhere.</summary>
public static class LoggingSetup
{
    public static WebApplicationBuilder AddSpineLogging(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, configuration) =>
        {
            configuration.ReadFrom.Configuration(context.Configuration).Enrich.FromLogContext();
            if (context.HostingEnvironment.IsDevelopment())
            {
                configuration.WriteTo.Console();
            }
            else
            {
                configuration.WriteTo.Console(new RenderedCompactJsonFormatter());
            }
        });
        return builder;
    }
}
