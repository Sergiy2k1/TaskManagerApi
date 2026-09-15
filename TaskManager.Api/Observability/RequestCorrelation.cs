using System.Diagnostics;
using Microsoft.Extensions.Primitives;

namespace TaskManager.Api.Observability;

internal static class RequestCorrelation
{
    public const string HeaderName =
        "X-Correlation-ID";

    public const string HttpContextItemKey =
        "TaskManager.CorrelationId";

    public const string TraceIdHttpContextItemKey =
        "TaskManager.TraceId";

    private const int MaxCorrelationIdLength = 64;

    public static string GetOrCreateCorrelationId(
        HttpRequest request)
    {
        if (request.Headers.TryGetValue(
                HeaderName,
                out var values) &&
            TryGetValidCorrelationId(
                values,
                out var correlationId))
        {
            return correlationId;
        }

        return Guid.NewGuid()
            .ToString("N");
    }

    public static string GetCorrelationId(
        HttpContext context)
    {
        return context.Items.TryGetValue(
                   HttpContextItemKey,
                   out var value) &&
               value is string correlationId
            ? correlationId
            : context.TraceIdentifier;
    }

    public static string GetTraceId(
        HttpContext context)
    {
        if (context.Items.TryGetValue(
                TraceIdHttpContextItemKey,
                out var value) &&
            value is string storedTraceId &&
            !string.IsNullOrWhiteSpace(
                storedTraceId))
        {
            return storedTraceId;
        }

        var activityTraceId =
            Activity.Current?.TraceId.ToString();

        return string.IsNullOrWhiteSpace(
                activityTraceId)
            ? context.TraceIdentifier
            : activityTraceId;
    }

    private static bool TryGetValidCorrelationId(
        StringValues values,
        out string correlationId)
    {
        correlationId = string.Empty;

        if (values.Count != 1)
        {
            return false;
        }

        var value =
            values[0];

        if (string.IsNullOrWhiteSpace(value) ||
            value.Length > MaxCorrelationIdLength)
        {
            return false;
        }

        foreach (var character in value)
        {
            if (!char.IsLetterOrDigit(character) &&
                character is not '-' and
                    not '_' and
                    not '.' and
                    not ':')
            {
                return false;
            }
        }

        correlationId = value;

        return true;
    }
}
