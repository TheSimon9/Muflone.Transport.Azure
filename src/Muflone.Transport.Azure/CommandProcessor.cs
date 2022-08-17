using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Muflone.Messages.Commands;
using Muflone.Transport.Azure.Abstracts;
using Muflone.Transport.Azure.Extensions;
using Muflone.Transport.Azure.Models;
using System.Globalization;
using Muflone.Factories;

namespace Muflone.Transport.Azure;

public class CommandProcessor<T> : ISubscriber<T>, ICommandProcessor, IAsyncDisposable where T : class, ICommand
{
    private readonly ServiceBusProcessor _processor;
    private readonly ICommandHandlerAsync<T> _commandHandlerAsync;
    private readonly IMessageSerializer _messageSerializer;
    private readonly ILogger<CommandProcessor<T>> _logger;

    public CommandProcessor(ServiceBusClient serviceBusClient,
        ICommandHandlerFactoryAsync commandHandlerFactory,
        IMessageSerializer? messageSerializer,
        AzureServiceBusConfiguration azureServiceBusConfiguration,
        ILogger<CommandProcessor<T>> logger)
    {
        _commandHandlerAsync = commandHandlerFactory.CreateCommandHandlerAsync<T>() ?? throw new ArgumentNullException(nameof(commandHandlerFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _messageSerializer = messageSerializer ?? new MessageSerializer();

        _processor = serviceBusClient.CreateProcessor(
            topicName: typeof(T).Name.ToLower(CultureInfo.InvariantCulture),
            subscriptionName: "", new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = false,
                MaxConcurrentCalls = azureServiceBusConfiguration.MaxConcurrentCalls
            });
        _processor.ProcessMessageAsync += MessageHandler;
        _processor.ProcessErrorAsync += ProcessErrorAsync;
    }

    public Task ProcessAsync<T>(T message, CancellationToken cancellationToken = default)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        return ProcessAsyncCore(message, cancellationToken);
    }

    private async Task ProcessAsyncCore<T>(T message, CancellationToken cancellationToken)
    {
        try
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            await _commandHandlerAsync.HandleAsync((dynamic)message, cancellationToken);
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

    private async Task MessageHandler(ProcessMessageEventArgs args)
    {
        try
        {
            _logger.LogInformation($"Received message '{args.Message.MessageId}'. Processing...");

            var message = await _messageSerializer.DeserializeAsync<T>(args.Message.Body.ToStream());

            await ProcessAsync((ICommand)message, args.CancellationToken);

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