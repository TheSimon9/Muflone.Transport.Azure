﻿using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Muflone.Messages;
using Muflone.Messages.Commands;
using Muflone.Transport.Azure.Abstracts;
using Muflone.Transport.Azure.Extensions;
using Muflone.Transport.Azure.Factories;

namespace Muflone.Transport.Azure;

public class ServiceBus : IServiceBus, IEventBus
{
    private readonly IServiceBusSenderFactory _senderFactory;
    private readonly ILogger<ServiceBus> _logger;
    private readonly IMessageSerializer _messageSerializer;

    public ServiceBus(IServiceBusSenderFactory senderFactory,
        ILogger<ServiceBus> logger)
    {
        _senderFactory = senderFactory ?? throw new ArgumentNullException(nameof(senderFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _messageSerializer = new MessageSerializer();
    }

    public Task SendAsync<T>(T command) where T : class, ICommand
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        return SendAsyncCore(command);
    }

    private async Task SendAsyncCore<T>(T command) where T : class, ICommand
    {
        var sender = _senderFactory.Create(command);
        _logger.LogInformation($"Send command '{command.MessageId}' to {sender.FullyQualifiedNamespace}/{sender.EntityPath}");

        var serializedMessage = _messageSerializer.Serialize(command);

        var correlationPair = command.UserProperties.FirstOrDefault(u => u.Key.Equals("CorrelationId"));
        var correlationId = string.Empty;
        if (correlationPair.Value != null)
            correlationId = correlationPair.Value.ToString();

        var busMessage = new ServiceBusMessage(serializedMessage)
        {
            CorrelationId = correlationId,
            MessageId = command.MessageId.ToString(),
            ApplicationProperties =
            {
                { "CommandName", command.GetType().FullName }
            }
        };

        await sender.SendMessageAsync(busMessage).ConfigureAwait(false);
    }

    public Task RegisterHandlerAsync<T>(Action<T> handler) where T : IMessage
    {
        return Task.CompletedTask;
    }

    public Task PublishAsync(IMessage @event)
    {
        if (@event == null)
            throw new ArgumentNullException(nameof(@event));

        return PublishAsyncCore(@event);
    }

    public async Task PublishAsyncCore(IMessage @event)
    {
        var sender = _senderFactory.Create(@event);
        _logger.LogInformation($"Publishing event '{@event.MessageId}' to {sender.FullyQualifiedNamespace}/{sender.EntityPath}");

        var serializedMessage = _messageSerializer.Serialize(@event);

        var correlationId = string.Empty;
        if (@event.UserProperties != null)
        {
            var correlationPair = @event.UserProperties.FirstOrDefault(u => u.Key.Equals("CorrelationId"));
            if (correlationPair.Value != null)
                correlationId = correlationPair.Value.ToString();
        }

        var busMessage = new ServiceBusMessage(serializedMessage)
        {
            CorrelationId = correlationId,
            MessageId = @event.MessageId.ToString(),
            ApplicationProperties =
            {
                { "EventName", @event.GetType().FullName }
            }
        };

        await sender.SendMessageAsync(busMessage).ConfigureAwait(false);
    }
}