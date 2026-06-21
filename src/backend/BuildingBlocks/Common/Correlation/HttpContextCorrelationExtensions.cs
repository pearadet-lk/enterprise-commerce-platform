using Microsoft.AspNetCore.Http;

namespace Common.Correlation;

public static class HttpContextCorrelationExtensions
{
    public static bool TryGetCorrelationId(this HttpContext context, out string correlationId)
    {
        if (context.Items.TryGetValue(CorrelationIdConstants.ItemKey, out var value)
            && value is string id
            && !string.IsNullOrWhiteSpace(id))
        {
            correlationId = id;
            return true;
        }

        correlationId = string.Empty;
        return false;
    }

    public static string GetCorrelationId(this HttpContext context) =>
        context.TryGetCorrelationId(out var correlationId)
            ? correlationId
            : string.Empty;
}
