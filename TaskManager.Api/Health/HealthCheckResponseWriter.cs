using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TaskManager.Api.Health;

public static class HealthCheckResponseWriter
{
    public static Task WriteAsync(
        HttpContext context,
        HealthReport report)
    {
        context.Response.ContentType =
            "application/json";

        var response =
            new HealthResponse(
                Status: report.Status.ToString(),
                TotalDurationMs:
                    Math.Round(
                        report.TotalDuration.TotalMilliseconds,
                        2),
                Checks:
                    report.Entries
                        .OrderBy(entry => entry.Key)
                        .Select(
                            entry =>
                                new HealthEntryResponse(
                                    Name: entry.Key,
                                    Status:
                                        entry.Value.Status.ToString(),
                                    DurationMs:
                                        Math.Round(
                                            entry.Value.Duration
                                                .TotalMilliseconds,
                                            2)))
                        .ToArray());

        return context.Response.WriteAsJsonAsync(
            response,
            cancellationToken:
                context.RequestAborted);
    }

    private sealed record HealthResponse(
        string Status,
        double TotalDurationMs,
        IReadOnlyList<HealthEntryResponse> Checks);

    private sealed record HealthEntryResponse(
        string Name,
        string Status,
        double DurationMs);
}
