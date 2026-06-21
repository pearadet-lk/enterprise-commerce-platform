using Common.Correlation;
using Microsoft.AspNetCore.Http;

namespace Common.Handlers;

public sealed class CorrelationIdDelegatingHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (!request.Headers.Contains(CorrelationIdConstants.HeaderName))
        {
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext?.TryGetCorrelationId(out var correlationId) == true)
            {
                request.Headers.TryAddWithoutValidation(CorrelationIdConstants.HeaderName, correlationId);
            }
        }

        return base.SendAsync(request, cancellationToken);
    }
}
