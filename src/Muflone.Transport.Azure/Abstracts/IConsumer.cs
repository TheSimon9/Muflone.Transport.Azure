using Muflone.Messages.Commands;
using Muflone.Messages.Events;

namespace Muflone.Transport.Azure.Abstracts;

public interface IConsumer
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);

    Task CommandConsumeAsync<T>(T message, CancellationToken cancellationToken = default) where T : class, ICommand;
    Task DomainEventConsumeAsync<T>(T message, CancellationToken cancellationToken = default) where T : class, IDomainEvent;
}