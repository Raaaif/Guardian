namespace PNETGuard.Models;
public sealed class DetectionRule
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "";
    public string Pattern { get; set; } = "";
    public string Severity { get; set; } = "review";
    public int ScoreImpact { get; set; }
    public string Description { get; set; } = "";
    public bool Enabled { get; set; } = true;
}
public sealed record GuardianEvaluation(int Score,string Classification,IReadOnlyList<string> Reasons);
