using System.Globalization;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Muflone.Messages;
using Muflone.Transport.Azure.Abstracts;
using Muflone.Transport.Azure.Extensions;
using Muflone.Transport.Azure.Models;

namespace Muflone.Transport.Azure;

public sealed class ServiceBusSubscriber<T> : ISubscriber<T>, IAsyncDisposable where T : IMessage
{
    private readonly ServiceBusProcessor _processor;
    private readonly IMessageProcessor _messageProcessor;
    private readonly IMessageSerializer _messageSerializer;
    private readonly ILogger <ServiceBusSubscriber<T>> _logger;

    public ServiceBusSubscriber(ServiceBusClient serviceBusClient,
        IMessageProcessor messageProcessor,
        IMessageSerializer messageSerializer,
        AzureServiceBusConfiguration azureServiceBusConfiguration,
        ILogger<ServiceBusSubscriber<T>> logger)
    {
        _messageProcessor = messageProcessor ?? throw new ArgumentNullException(nameof(messageProcessor));
        _messageSerializer = messageSerializer ?? throw new ArgumentNullException(nameof(messageSerializer));

        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

    private async Task MessageHandler(ProcessMessageEventArgs args)
    {
        try
        {
            _logger.LogInformation($"Received message '{args.Message.MessageId}'. Processing...");

            var message = await _messageSerializer.DeserializeAsync<T>(args.Message.Body.ToStream());

            await _messageProcessor.ProcessAsync((dynamic)message, args.CancellationToken);

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

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await _processor.StartProcessingAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}