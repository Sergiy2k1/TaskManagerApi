using System.Diagnostics;

namespace TaskManager.Api.Observability;

public sealed partial class RequestCorrelationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestCorrelationMiddleware> _logger;

    public RequestCorrelationMiddleware(
        RequestDelegate next,
        ILogger<RequestCorrelationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context)
    {
        var correlationId =
            RequestCorrelation.GetOrCreateCorrelationId(
                context.Request);

        context.Items[
            RequestCorrelation.HttpContextItemKey] =
            correlationId;

        context.Response.Headers[
            RequestCorrelation.HeaderName] =
            correlationId;

        var traceId =
            RequestCorrelation.GetTraceId(
                context);

        context.Items[
            RequestCorrelation.TraceIdHttpContextItemKey] =
            traceId;

        var startedAt =
            Stopwatch.GetTimestamp();

        using var scope =
            _logger.BeginScope(
                new Dictionary<string, object?>
                {
                    ["CorrelationId"] = correlationId,
                    ["TraceId"] = traceId
                });

        await _next(context);

        var elapsedMilliseconds =
            Stopwatch.GetElapsedTime(
                    startedAt)
                .TotalMilliseconds;

        if (context.Request.Path.StartsWithSegments(
                "/health"))
        {
            LogHealthRequestCompleted(
                _logger,
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                elapsedMilliseconds,
                correlationId,
                traceId);

            return;
        }

        LogRequestCompleted(
            _logger,
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode,
            elapsedMilliseconds,
            correlationId,
            traceId);
    }

    [LoggerMessage(
        EventId = 100,
        Level = LogLevel.Information,
        Message =
            "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMilliseconds} ms. CorrelationId: {CorrelationId}. TraceId: {TraceId}")]
    private static partial void LogRequestCompleted(
        ILogger logger,
        string method,
        PathString path,
        int statusCode,
        double elapsedMilliseconds,
        string correlationId,
        string traceId);

    [LoggerMessage(
        EventId = 101,
        Level = LogLevel.Debug,
        Message =
            "Health probe {Method} {Path} responded {StatusCode} in {ElapsedMilliseconds} ms. CorrelationId: {CorrelationId}. TraceId: {TraceId}")]
    private static partial void LogHealthRequestCompleted(
        ILogger logger,
        string method,
        PathString path,
        int statusCode,
        double elapsedMilliseconds,
        string correlationId,
        string traceId);
}
