namespace PNETGuard.Models;
public sealed record PreMatchScanResult(
    string ScanId, DateTimeOffset StartedAt, DateTimeOffset FinishedAt,
    int ProcessesAnalyzed, int GameModulesAnalyzed, int GameFilesAnalyzed,
    bool CounterStrikeDetected, bool SteamInstallationValidated, bool AccessLimited,
    IReadOnlyList<AntiCheatFinding> Findings, string ReportPath, string ManifestPath,
    int Score, string Classification, IReadOnlyList<string> ScoreReasons)
{
    public bool IsClean => Classification == "approved";
    public TimeSpan Duration => FinishedAt - StartedAt;
}
