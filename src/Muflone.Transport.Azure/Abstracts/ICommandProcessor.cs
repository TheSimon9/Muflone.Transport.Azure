using Muflone.Messages;

namespace Muflone.Transport.Azure.Abstracts;

public interface ICommandProcessor
{
    Task ProcessAsync<T>(T message, CancellationToken cancellationToken = default);
}