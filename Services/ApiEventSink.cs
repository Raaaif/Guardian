using System.Net.Http.Json;
using PNETGuard.Models;

namespace PNETGuard.Services;

/// <summary>
/// Estrutura pronta para integração futura com a API/banco da PNET.
/// Deixe Enabled=false enquanto não houver backend.
/// Nunca coloque a chave administrativa do banco dentro do aplicativo.
/// </summary>
public sealed class ApiEventSink : IEventSink
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(5) };
    public bool Enabled { get; init; }
    public string Endpoint { get; init; } = "https://SEU-DOMINIO/api/guard/events";
    public string? SessionToken { get; init; }

    public async Task WriteAsync(GuardEvent guardEvent, CancellationToken cancellationToken = default)
    {
        if (!Enabled) return;
        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint)
        {
            Content = JsonContent.Create(guardEvent)
        };
        if (!string.IsNullOrWhiteSpace(SessionToken))
            request.Headers.Authorization = new("Bearer", SessionToken);
        using HttpResponseMessage response = await _http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public ValueTask DisposeAsync()
    {
        _http.Dispose();
        return ValueTask.CompletedTask;
    }
}
