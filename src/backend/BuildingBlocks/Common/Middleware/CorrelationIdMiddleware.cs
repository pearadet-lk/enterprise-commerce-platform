using System.Diagnostics;
using Common.Correlation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Common.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context);

        context.Items[CorrelationIdConstants.ItemKey] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdConstants.HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        var activity = Activity.Current;
        activity?.SetTag(CorrelationIdConstants.OtelTagName, correlationId);
        activity?.SetBaggage(CorrelationIdConstants.BaggageName, correlationId);

        using (logger.BeginScope(new Dictionary<string, object?>
        {
            [CorrelationIdConstants.LogPropertyName] = correlationId
        }))
        {
            await next(context);
        }
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        var headerValue = context.Request.Headers[CorrelationIdConstants.HeaderName].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(headerValue))
        {
            return headerValue;
        }

        return Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
    }
}
