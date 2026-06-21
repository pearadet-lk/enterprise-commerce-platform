using Common.Handlers;
using Common.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;

namespace Common.Extensions;

public static class PlatformExtensions
{
    public static IHostApplicationBuilder AddPlatformCrossCutting(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddTransient<CorrelationIdDelegatingHandler>();
        builder.Services.ConfigureAll<HttpClientFactoryOptions>(options =>
        {
            options.HttpMessageHandlerBuilderActions.Add(handlerBuilder =>
            {
                handlerBuilder.AdditionalHandlers.Add(
                    handlerBuilder.Services.GetRequiredService<CorrelationIdDelegatingHandler>());
            });
        });

        return builder;
    }

    public static WebApplication UsePlatformMiddleware(this WebApplication app)
    {
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseMiddleware<CorrelationIdMiddleware>();
        return app;
    }
}
