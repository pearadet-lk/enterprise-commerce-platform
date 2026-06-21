using System.Diagnostics;
using Common.Correlation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Common.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.GetCorrelationId();
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
        }

        logger.LogError(
            exception,
            "Unhandled exception. {CorrelationId} {Method} {Path}",
            correlationId,
            context.Request.Method,
            context.Request.Path);

        if (context.Response.HasStarted)
        {
            throw exception;
        }

        var includeDetails = environment.IsDevelopment() || environment.EnvironmentName == "Docker";

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred.",
            Detail = includeDetails ? exception.Message : "Please contact support and provide the correlation id.",
            Instance = context.Request.Path
        };

        problem.Extensions["correlationId"] = correlationId;
        if (includeDetails)
        {
            problem.Extensions["exceptionType"] = exception.GetType().Name;
        }

        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.Headers[CorrelationIdConstants.HeaderName] = correlationId;
        await context.Response.WriteAsJsonAsync(problem);
    }
}
