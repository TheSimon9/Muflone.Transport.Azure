using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Muflone.Messages.Commands;
using Muflone.Messages.Events;
using Muflone.Transport.Azure.Abstracts;
using Muflone.Transport.Azure.Extensions;
using Muflone.Transport.Azure.Models;
using System.Globalization;
using Muflone.Transport.Azure.Factories;

namespace Muflone.Transport.Azure.Consumers;

public abstract class DomainEventConsumerBase<T> : IConsumer, IAsyncDisposable where T : class, IDomainEvent
{
    private readonly ServiceBusProcessor _processor;
    private readonly IMessageSerializer _messageSerializer;
    private readonly ILogger _logger;

    protected abstract IEnumerable<IDomainEventHandlerAsync<T>> DomainEventsHanderAsync { get; }

    protected DomainEventConsumerBase(AzureServiceBusConfiguration azureServiceBusConfiguration,
        ILoggerFactory loggerFactory,
        IMessageSerializer? messageSerializer = null)
    {
        _logger = loggerFactory.CreateLogger(GetType()) ?? throw new ArgumentNullException(nameof(loggerFactory));
        _messageSerializer = messageSerializer ?? new MessageSerializer();

        if (string.IsNullOrWhiteSpace(azureServiceBusConfiguration.SubscriptionName))
            throw new ArgumentNullException(nameof(azureServiceBusConfiguration.SubscriptionName));

        // Create Topic on Azure ServiceBus if missing
        ServiceBusAdministrator.CreateTopicIfNotExistAsync(azureServiceBusConfiguration).GetAwaiter().GetResult();

        var serviceBusClient = new ServiceBusClient(azureServiceBusConfiguration.ConnectionString);
        _processor = serviceBusClient.CreateProcessor(
            topicName: typeof(T).Name.ToLower(CultureInfo.InvariantCulture),
            subscriptionName: azureServiceBusConfiguration.SubscriptionName, new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = false,
                MaxConcurrentCalls = azureServiceBusConfiguration.MaxConcurrentCalls
            });
        _processor.ProcessMessageAsync += AzureMessageHandler;
        _processor.ProcessErrorAsync += ProcessErrorAsync;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default) =>
        await _processor.StartProcessingAsync(cancellationToken).ConfigureAwait(false);

    public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task CommandConsumeAsync<T>(T message, CancellationToken cancellationToken = default) where T : class, ICommand
    {
        return Task.CompletedTask;
    }

    public async Task DomainEventConsumeAsync<T1>(T1 message, CancellationToken cancellationToken = default) where T1 : class, IDomainEvent
    {
        try
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            foreach (var eventHandlerAsync in DomainEventsHanderAsync)
            {
                await eventHandlerAsync.HandleAsync((dynamic)message, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                $"An error occurred processing domainEvent {typeof(T).Name}. StackTrace: {ex.StackTrace} - Source: {ex.Source} - Message: {ex.Message}");
            throw;
        }
    }

    private async Task AzureMessageHandler(ProcessMessageEventArgs args)
    {
        try
        {
            _logger.LogInformation($"Received message '{args.Message.MessageId}'. Processing...");

            var message = _messageSerializer.Deserialize<T>(args.Message.Body.ToArray());

            await DomainEventConsumeAsync((IDomainEvent)message, args.CancellationToken);

            await args.CompleteMessageAsync(args.Message).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"an error has occurred while processing message '{args.Message.MessageId}': {ex.Message}");
            if (args.Message.DeliveryCount > 3)
                await args.DeadLetterMessageAsync(args.Message).ConfigureAwait(false);
            else
                await args.AbandonMessageAsync(args.Message).ConfigureAwait(false);
        }
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs arg)
    {
        _logger.LogError(arg.Exception, $"An exception has occurred while processing message '{arg.FullyQualifiedNamespace}'");
        return Task.CompletedTask;
    }

    #region Dispose
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    #endregion
}