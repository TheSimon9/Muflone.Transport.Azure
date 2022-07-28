using System.Collections.Concurrent;
using Azure.Messaging.ServiceBus;
using Muflone.Messages;
using Muflone.Transport.Azure.Models;

namespace Muflone.Transport.Azure.Factories;

public class ServiceBusSenderFactory : IAsyncDisposable, IServiceBusSenderFactory
{
    private readonly IAzureQueueReferenceFactory _queueReferenceFactory;
    private readonly ServiceBusClient _serviceBusClient;
    private readonly ConcurrentDictionary<AzureQueueReferences, ServiceBusSender> _senders = new();

    public ServiceBusSenderFactory(IAzureQueueReferenceFactory queueReferenceFactory,
        ServiceBusClient serviceBusClient)
    {
        _queueReferenceFactory = queueReferenceFactory ?? throw new ArgumentNullException(nameof(queueReferenceFactory));
        _serviceBusClient = serviceBusClient ?? throw new ArgumentNullException(nameof(serviceBusClient));
    }

    public ServiceBusSender Create<T>(T message) where T : IMessage
    {
        var references = _queueReferenceFactory.Create<T>();
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