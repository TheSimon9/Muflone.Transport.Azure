using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Muflone.Messages.Commands;
using Muflone.Transport.Azure.Abstracts;
using Muflone.Transport.Azure.Extensions;
using Muflone.Transport.Azure.Models;
using System.Globalization;
using Muflone.Messages.Events;
using Muflone.Transport.Azure.Factories;

namespace Muflone.Transport.Azure.Consumers;

/// <summary>
/// TODO: Unify Consumer: ora ci sono Consumer diversi per Command e DomainEvent
/// TODO: Rimuovere stringhe di connessione e subscriptioname dal codice
/// </summary>
/// <typeparam name="T"></typeparam>

public abstract class CommandConsumerBase<T> : IConsumer, IAsyncDisposable where T : class, ICommand
{
    public string TopicName { get; }

    private readonly ServiceBusProcessor _processor;
    private readonly IMessageSerializer _messageSerializer;
    private readonly ILogger _logger;

    protected abstract ICommandHandlerAsync<T> CommandHandlerAsync { get; }

    protected CommandConsumerBase(AzureServiceBusConfiguration azureServiceBusConfiguration,
        ILoggerFactory loggerFactory,
        IMessageSerializer? messageSerializer = null)
    {
        TopicName = typeof(T).Name;

        _logger = loggerFactory.CreateLogger(GetType()) ?? throw new ArgumentNullException(nameof(loggerFactory));

        var serviceBusClient = new ServiceBusClient(azureServiceBusConfiguration.ConnectionString);
        _messageSerializer = messageSerializer ?? new MessageSerializer();

        // Create Queue on Azure ServiceBus if missing
        ServiceBusAdministrator.CreateQueueIfNotExistAsync(new AzureQueueReferences(typeof(T).Name, "",
            azureServiceBusConfiguration.ConnectionString)).GetAwaiter().GetResult();

        _processor = serviceBusClient.CreateProcessor(
            topicName: typeof(T).Name.ToLower(CultureInfo.InvariantCulture),
            subscriptionName: "", new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = false,
                MaxConcurrentCalls = azureServiceBusConfiguration.MaxConcurrentCalls
            });
        _processor.ProcessMessageAsync += AzureMessageHandler;
        _processor.ProcessErrorAsync += ProcessErrorAsync;
    }

    public Task CommandConsumeAsync<T>(T message, CancellationToken cancellationToken = default) where T : class, ICommand
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        return ConsumeAsyncCore(message, cancellationToken);
    }

    public Task DomainEventConsumeAsync<T1>(T1 message, CancellationToken cancellationToken = default) where T1 : class, IDomainEvent
    {
        return Task.CompletedTask;
    }

    private async Task ConsumeAsyncCore<T>(T message, CancellationToken cancellationToken)
    {
        try
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            await CommandHandlerAsync.HandleAsync((dynamic)message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"An error occurred processing command {typeof(T).Name}. StackTrace: {ex.StackTrace} - Source: {ex.Source} - Message: {ex.Message}");
            throw;
        }
    }
    public async Task StartAsync(CancellationToken cancellationToken = default) =>
        await _processor.StartProcessingAsync(cancellationToken).ConfigureAwait(false);

    public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    private async Task AzureMessageHandler(ProcessMessageEventArgs args)
    {
        try
        {
            _logger.LogInformation($"Received message '{args.Message.MessageId}'. Processing...");

            var message = _messageSerializer.Deserialize<T>(args.Message.Body.ToArray());

            await CommandConsumeAsync((ICommand)message, args.CancellationToken);

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