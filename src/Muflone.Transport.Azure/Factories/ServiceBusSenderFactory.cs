using System.Collections.Concurrent;
using Azure.Messaging.ServiceBus;
using Muflone.Messages;
using Muflone.Transport.Azure.Models;

namespace Muflone.Transport.Azure.Factories;

public class ServiceBusSenderFactory : IAsyncDisposable, IServiceBusSenderFactory
{
    private readonly ServiceBusClient _serviceBusClient;
    private readonly ConcurrentDictionary<AzureQueueReferences, ServiceBusSender> _senders = new();

    private readonly IEnumerable<AzureServiceBusConfiguration> _configurations;

    public ServiceBusSenderFactory(ServiceBusClient serviceBusClient,
        IEnumerable<AzureServiceBusConfiguration> configurations)
    {
        _serviceBusClient = serviceBusClient ?? throw new ArgumentNullException(nameof(serviceBusClient));
        _configurations = configurations;
    }

    public ServiceBusSender Create<T>(T message) where T : IMessage
    {
        var configuration = _configurations.FirstOrDefault(c => c.TopicName.Equals(message.GetType().Name));

        var references = new AzureQueueReferences(message.GetType().Name, $"{configuration!.ClientId}-subscription",
            configuration!.ConnectionString);
        var sender = _senders.GetOrAdd(references, _ => _serviceBusClient.CreateSender(references.TopicName));
        
        if (sender is null || sender.IsClosed)
            sender = _senders[references] = _serviceBusClient.CreateSender(references.TopicName);

        return sender;
    }

    public ServiceBusSender Create(IMessage message)
    {
        var configuration = _configurations.FirstOrDefault(c => c.TopicName.Equals(message.GetType().Name));
        var references = new AzureQueueReferences(message.GetType().Name, $"{configuration!.ClientId}-subscription", 
            configuration!.ConnectionString);

        var sender = _senders.GetOrAdd(references, _ => _serviceBusClient.CreateSender(references.TopicName));

        if (sender is null || sender.IsClosed)
            sender = _senders[references] = _serviceBusClient.CreateSender(references.TopicName);

        return sender;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var sender in _senders.Values)
            await sender.DisposeAsync();

        _senders.Clear();
    }
}