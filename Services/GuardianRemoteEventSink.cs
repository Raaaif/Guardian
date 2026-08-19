using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using PNETGuard.Models;

namespace PNETGuard.Services;

public sealed class GuardianRemoteEventSink : IEventSink
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };

    private static readonly string LogFolder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Guardian", "Logs");

    public static string RemoteLogPath =>
        Path.Combine(LogFolder, "supabase.log");

    public async Task WriteAsync(
        GuardEvent guardEvent,
        CancellationToken cancellationToken = default)
    {
        if (!GuardianServerConfig.IsConfigured)
        {
            await LogAsync(
                $"IGNORADO | {guardEvent.Type} | GuardianServerConfig.cs não está configurado.");
            return;
        }

        string endpoint =
            $"{GuardianServerConfig.SupabaseUrl.TrimEnd('/')}/rest/v1/{GuardianServerConfig.EventsTable}";

        var payload = new
        {
            event_id = Guid.NewGuid(),
            session_id = guardEvent.SessionId,
            event_type = guardEvent.Type,
            event_time = guardEvent.Timestamp,
            app_version = Application.ProductVersion,
            computer_name = Environment.MachineName,
            data = guardEvent.Data
        };

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = JsonContent.Create(payload)
            };

            request.Headers.TryAddWithoutValidation(
                "apikey", GuardianServerConfig.SupabaseAnonKey);
            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer", GuardianServerConfig.SupabaseAnonKey);
            request.Headers.TryAddWithoutValidation("Prefer", "return=minimal");

            using HttpResponseMessage response =
                await _http.SendAsync(request, cancellationToken);

            string responseBody =
                await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string error =
                    $"ERRO | {guardEvent.Type} | HTTP {(int)response.StatusCode} " +
                    $"{response.ReasonPhrase} | {responseBody}";
                await LogAsync(error);
                throw new HttpRequestException(error);
            }

            await LogAsync(
                $"OK | {guardEvent.Type} | Sessão={guardEvent.SessionId}");
        }
        catch (Exception ex)
        {
            await LogAsync(
                $"FALHA | {guardEvent.Type} | {ex.GetType().Name}: {ex.Message}");
            throw;
        }
    }

    private static async Task LogAsync(string message)
    {
        try
        {
            Directory.CreateDirectory(LogFolder);
            await File.AppendAllTextAsync(
                RemoteLogPath,
                $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz} | {message}{Environment.NewLine}",
                Encoding.UTF8);
        }
        catch
        {
            // O log nunca pode interromper o Guardian.
        }
    }

    public ValueTask DisposeAsync()
    {
        _http.Dispose();
        return ValueTask.CompletedTask;
    }
}
