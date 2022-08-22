using Muflone.Messages;

namespace Muflone.Transport.Azure.Abstracts;

public interface ISubscriber
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}

public interface ISubscriber<T> : ISubscriber where T : IMessage
{ }