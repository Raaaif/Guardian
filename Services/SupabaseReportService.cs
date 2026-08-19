using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using PNETGuard.Models;

namespace PNETGuard.Services;

public sealed class SupabaseReportService : IDisposable
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };
    private readonly DatabaseSettings _settings;

    public SupabaseReportService(DatabaseSettings settings) => _settings = settings;

    private string Endpoint => $"{_settings.SupabaseUrl.TrimEnd('/')}/rest/v1/{_settings.TableName}";

    private void AddHeaders(HttpRequestMessage request)
    {
        request.Headers.TryAddWithoutValidation("apikey", _settings.AnonKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.AnonKey);
    }

    public async Task TestAsync(CancellationToken cancellationToken = default)
    {
        Validate();
        using var request = new HttpRequestMessage(HttpMethod.Get, Endpoint + "?select=id&limit=1");
        AddHeaders(request);
        using var response = await _http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task UploadScanAsync(SessionInfo session, PreMatchScanResult result, CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled) return;
        Validate();
        JsonElement scanJson = JsonSerializer.Deserialize<JsonElement>(await File.ReadAllTextAsync(result.ReportPath, cancellationToken));
        var payload = new
        {
            scan_id = result.ScanId,
            session_id = (string?)null,
            nickname = session.Nickname,
            steam_id = session.SteamId,
            app_version = Application.ProductVersion,
            result = result.IsClean ? "approved" : (result.AccessLimited ? "incomplete" : "review"),
            scan_report = scanJson,
            session_report = (object?)null
        };
        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint)
        {
            Content = JsonContent.Create(payload)
        };
        AddHeaders(request);
        request.Headers.TryAddWithoutValidation("Prefer", "resolution=merge-duplicates,return=minimal");
        using var response = await _http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private void Validate()
    {
        if (!Uri.TryCreate(_settings.SupabaseUrl, UriKind.Absolute, out _)) throw new InvalidOperationException("URL do Supabase inválida.");
        if (string.IsNullOrWhiteSpace(_settings.AnonKey)) throw new InvalidOperationException("Chave pública anon não informada.");
        if (string.IsNullOrWhiteSpace(_settings.TableName)) throw new InvalidOperationException("Tabela não informada.");
    }

    public void Dispose() => _http.Dispose();
}
