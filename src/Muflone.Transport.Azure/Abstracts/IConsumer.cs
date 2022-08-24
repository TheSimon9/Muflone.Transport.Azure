using Muflone.Messages.Commands;
using Muflone.Messages.Events;

namespace Muflone.Transport.Azure.Abstracts;

public interface IConsumer
{
	string TopicName { get; }
	Task StartAsync(CancellationToken cancellationToken = default);
	Task StopAsync(CancellationToken cancellationToken = default);
}

public interface IDomainEventConsumer : IConsumer
{
	Task DomainEventConsumeAsync<T>(T message, CancellationToken cancellationToken = default)
		where T : class, IDomainEvent;
}

public interface IIntegrationEventConsumer : IConsumer
{
	Task IntegrationEventConsumeAsync<T>(T message, CancellationToken cancellationToken = default)
		where T : class, IIntegrationEvent;
}

public interface ICommandConsumer : IConsumer
{
	Task CommandConsumeAsync<T>(T message, CancellationToken cancellationToken = default) where T : class, ICommand;
}