using PNETGuard.Models;

namespace PNETGuard.Services;

public interface IEventSink : IAsyncDisposable
{
    Task WriteAsync(GuardEvent guardEvent, CancellationToken cancellationToken = default);
}
