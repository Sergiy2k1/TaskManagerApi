namespace TaskManager.Api.Contracts;

public sealed record PingResponse(
    string Message,
    DateTimeOffset TimestampUtc);