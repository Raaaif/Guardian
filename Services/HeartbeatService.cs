using PNETGuard.Models;

namespace PNETGuard.Services;

public sealed class HeartbeatService : IAsyncDisposable
{
    private readonly IEventSink _sink;
    private readonly SessionInfo _session;
    private readonly Func<object> _statusProvider;
    private CancellationTokenSource? _cts;
    private Task? _loop;

    public HeartbeatService(IEventSink sink, SessionInfo session, Func<object> statusProvider)
    {
        _sink = sink;
        _session = session;
        _statusProvider = statusProvider;
    }

    public void Start()
    {
        if (_loop is not null)
            return;

        _cts = new CancellationTokenSource();
        _loop = Task.Run(() => RunAsync(_cts.Token));
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await _sink.WriteAsync(new GuardEvent(
                "heartbeat",
                DateTimeOffset.UtcNow,
                _session.SessionId,
                _statusProvider()),
                cancellationToken);

            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
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
}
