using PNETGuard.Models;

namespace PNETGuard.Services;

public sealed class CompositeEventSink : IEventSink
{
    private readonly IReadOnlyList<IEventSink> _sinks;
    public CompositeEventSink(params IEventSink[] sinks) => _sinks = sinks;

    public async Task WriteAsync(GuardEvent guardEvent, CancellationToken cancellationToken = default)
    {
        foreach (IEventSink sink in _sinks)
        {
            try { await sink.WriteAsync(guardEvent, cancellationToken); }
            catch { /* Uma falha remota nunca deve interromper o monitoramento local. */ }
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (IEventSink sink in _sinks)
            await sink.DisposeAsync();
    }
}
