namespace PNETGuard.Models;

public enum ReportDestinationType
{
    LocalOnly = 0,
    Supabase = 1,
    ApiWebhook = 2
}

public sealed class DatabaseSettings
{
    public bool Enabled { get; set; }
    public string OrganizationName { get; set; } = "";
    public ReportDestinationType DestinationType { get; set; } = ReportDestinationType.LocalOnly;

    public string SupabaseUrl { get; set; } = "";
    public string AnonKey { get; set; } = "";
    public string TableName { get; set; } = "guard_reports";

    public string ApiUrl { get; set; } = "";
    public string ApiToken { get; set; } = "";
    public string ApiTokenHeader { get; set; } = "Authorization";
}
