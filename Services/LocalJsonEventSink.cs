using System.Text.Json;
using PNETGuard.Models;

namespace PNETGuard.Services;

public sealed class LocalJsonEventSink : IEventSink
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly StreamWriter _writer;

    public string FilePath { get; }

    public LocalJsonEventSink(string sessionId)
    {
        string folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Guardian",
            "Sessions");

        Directory.CreateDirectory(folder);
        FilePath = Path.Combine(folder, $"{sessionId}.jsonl");

        _writer = new StreamWriter(
            new FileStream(FilePath, FileMode.Append, FileAccess.Write, FileShare.Read),
            new System.Text.UTF8Encoding(false))
        {
            AutoFlush = true
        };
    }

    public async Task WriteAsync(GuardEvent guardEvent, CancellationToken cancellationToken = default)
    {
        string json = JsonSerializer.Serialize(guardEvent);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            await _writer.WriteLineAsync(json.AsMemory(), cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _writer.DisposeAsync();
        _gate.Dispose();
    }
}
