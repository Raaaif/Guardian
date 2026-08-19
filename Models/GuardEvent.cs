namespace PNETGuard.Models;

public sealed record GuardEvent(
    string Type,
    DateTimeOffset Timestamp,
    string SessionId,
    object Data
);
