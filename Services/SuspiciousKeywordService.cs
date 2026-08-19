namespace PNETGuard.Services;

public static class SuspiciousKeywordService
{
    public static readonly string[] StrongKeywords =
    {
        "cheat",
        "cheatengine",
        "extremeinjector",
        "dllinjector",
        "injector",
        "wallhack",
        "aimbot",
        "triggerbot",
        "ragebot",
        "silentaim",
        "speedhack"
    };

    public static string? Find(string value)
    {
        string normalized = Normalize(value);

        return StrongKeywords.FirstOrDefault(keyword =>
            normalized.Contains(Normalize(keyword),
                StringComparison.OrdinalIgnoreCase));
    }

    public static string Normalize(string value) =>
        value.Replace("_", "")
             .Replace("-", "")
             .Replace(" ", "")
             .ToLowerInvariant();
}
