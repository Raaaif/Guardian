namespace PNETGuard.Models;

public sealed record AntiCheatFinding(
    string Code,
    string Severity,
    string Category,
    string Summary,
    string? TechnicalDetail,
    DateTimeOffset Timestamp
);
