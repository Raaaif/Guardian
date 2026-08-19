using System.Diagnostics;
using PNETGuard.Models;

namespace PNETGuard.Services;

public sealed class ProcessMonitor : IAsyncDisposable
{
    private readonly IEventSink _sink;
    private readonly SessionInfo _session;
    private readonly TimeSpan _pollInterval;
    private readonly Dictionary<int, ProcessSnapshot> _known = new();
    private CancellationTokenSource? _cts;
    private Task? _loop;

    public event Action<bool>? GameStateChanged;
    public event Action<int>? ProcessCountChanged;

    public ProcessMonitor(
        IEventSink sink,
        SessionInfo session,
        TimeSpan? pollInterval = null)
    {
        _sink = sink;
        _session = session;
        _pollInterval = pollInterval ?? TimeSpan.FromSeconds(3);
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_loop is not null)
            return;

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        foreach (ProcessSnapshot process in ReadProcesses())
            _known[process.Id] = process;

        await _sink.WriteAsync(new GuardEvent(
            "process_snapshot",
            DateTimeOffset.UtcNow,
            _session.SessionId,
            new
            {
                Count = _known.Count,
                Processes = _known.Values
                    .OrderBy(p => p.Name)
                    .Select(p => new { p.Id, p.Name, p.Path })
            }), _cts.Token);

        _loop = Task.Run(() => RunAsync(_cts.Token), _cts.Token);
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        bool? previousGameState = null;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                Dictionary<int, ProcessSnapshot> current = ReadProcesses()
                    .ToDictionary(p => p.Id);

                foreach (ProcessSnapshot started in current.Values.Where(p => !_known.ContainsKey(p.Id)))
                {
                    await _sink.WriteAsync(new GuardEvent(
                        "process_started",
                        DateTimeOffset.UtcNow,
                        _session.SessionId,
                        new { started.Id, started.Name, started.Path }),
                        cancellationToken);
                }

                foreach (ProcessSnapshot stopped in _known.Values.Where(p => !current.ContainsKey(p.Id)))
                {
                    await _sink.WriteAsync(new GuardEvent(
                        "process_stopped",
                        DateTimeOffset.UtcNow,
                        _session.SessionId,
                        new { stopped.Id, stopped.Name, stopped.Path }),
                        cancellationToken);
                }

                _known.Clear();
                foreach (var pair in current)
                    _known[pair.Key] = pair.Value;

                bool gameRunning = current.Values.Any(p =>
                    p.Name.Equals("hl", StringComparison.OrdinalIgnoreCase) ||
                    p.Name.Equals("hl.exe", StringComparison.OrdinalIgnoreCase));

                if (previousGameState != gameRunning)
                {
                    previousGameState = gameRunning;
                    GameStateChanged?.Invoke(gameRunning);

                    await _sink.WriteAsync(new GuardEvent(
                        "game_state",
                        DateTimeOffset.UtcNow,
                        _session.SessionId,
                        new { Running = gameRunning, Process = "hl.exe" }),
                        cancellationToken);
                }

                ProcessCountChanged?.Invoke(current.Count);
                await Task.Delay(_pollInterval, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                await _sink.WriteAsync(new GuardEvent(
                    "monitor_error",
                    DateTimeOffset.UtcNow,
                    _session.SessionId,
                    new { ex.Message }),
                    cancellationToken);

                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }
    }

    private static IEnumerable<ProcessSnapshot> ReadProcesses()
    {
        foreach (Process process in Process.GetProcesses())
        {
            ProcessSnapshot? snapshot = null;

            try
            {
                string name = process.ProcessName;
                string? path = null;

                try
                {
                    path = process.MainModule?.FileName;
                }
                catch
                {
                    // Alguns processos do Windows bloqueiam a leitura sem privilégios.
                }

                snapshot = new ProcessSnapshot(process.Id, name, path);
            }
            catch
            {
                // O processo pode ter sido encerrado durante a enumeração.
            }
            finally
            {
                process.Dispose();
            }

            if (snapshot is not null)
                yield return snapshot;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_cts is null)
            return;

        _cts.Cancel();

        if (_loop is not null)
        {
            try { await _loop; }
            catch (OperationCanceledException) { }
        }

        _cts.Dispose();
        _cts = null;
        _loop = null;
    }

    private sealed record ProcessSnapshot(int Id, string Name, string? Path);
}
