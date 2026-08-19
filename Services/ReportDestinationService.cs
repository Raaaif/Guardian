using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using PNETGuard.Models;

namespace PNETGuard.Services;

public sealed class ReportDestinationService : IDisposable
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(20) };
    private readonly DatabaseSettings _settings;
    public ReportDestinationService(DatabaseSettings settings) => _settings = settings;

    public async Task TestAsync(CancellationToken cancellationToken=default)
    {
        if (!_settings.Enabled || _settings.DestinationType == ReportDestinationType.LocalOnly) return;
        if (_settings.DestinationType == ReportDestinationType.Supabase)
        {
            using var service = new SupabaseReportService(_settings); await service.TestAsync(cancellationToken); return;
        }
        ValidateApi();
        using var request = new HttpRequestMessage(HttpMethod.Post, _settings.ApiUrl)
        { Content = JsonContent.Create(new { type="guardian_connection_test", organization=_settings.OrganizationName, sent_at_utc=DateTimeOffset.UtcNow }) };
        AddApiToken(request); using var response = await _http.SendAsync(request,cancellationToken); response.EnsureSuccessStatusCode();
    }

    public async Task UploadScanAsync(SessionInfo session, PreMatchScanResult result, CancellationToken cancellationToken=default)
    {
        if (!_settings.Enabled || _settings.DestinationType == ReportDestinationType.LocalOnly) return;
        if (_settings.DestinationType == ReportDestinationType.Supabase)
        { using var service=new SupabaseReportService(_settings); await service.UploadScanAsync(session,result,cancellationToken); return; }
        ValidateApi();
        JsonElement scan=JsonSerializer.Deserialize<JsonElement>(await File.ReadAllTextAsync(result.ReportPath,cancellationToken));
        using var request=new HttpRequestMessage(HttpMethod.Post,_settings.ApiUrl){Content=JsonContent.Create(new{type="guardian_scan_report",organization=_settings.OrganizationName,scan_id=result.ScanId,session_id=session.SessionId,nickname=session.Nickname,steam_id=session.SteamId,score=result.Score,classification=result.Classification,report=scan})};
        AddApiToken(request); using var response=await _http.SendAsync(request,cancellationToken); response.EnsureSuccessStatusCode();
    }
    private void AddApiToken(HttpRequestMessage request){if(string.IsNullOrWhiteSpace(_settings.ApiToken))return;string h=string.IsNullOrWhiteSpace(_settings.ApiTokenHeader)?"Authorization":_settings.ApiTokenHeader;if(h.Equals("Authorization",StringComparison.OrdinalIgnoreCase))request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",_settings.ApiToken);else request.Headers.TryAddWithoutValidation(h,_settings.ApiToken);}
    private void ValidateApi(){if(!Uri.TryCreate(_settings.ApiUrl,UriKind.Absolute,out var uri)||uri.Scheme!=Uri.UriSchemeHttps)throw new InvalidOperationException("Informe uma URL HTTPS válida para a API/Webhook.");}
    public void Dispose()=>_http.Dispose();
}
