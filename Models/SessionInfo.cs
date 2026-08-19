namespace PNETGuard.Models;

public sealed record SessionInfo(
    string SessionId,
    string Nickname,
    string SteamId,
    string? CsFolder,
    DateTimeOffset StartedAt
);
