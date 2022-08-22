using Muflone.Messages;

namespace Muflone.Transport.Azure.Abstracts;

public interface IMessageProcessor
{
    Task ProcessAsync<T>(T message, CancellationToken cancellationToken = default);
}