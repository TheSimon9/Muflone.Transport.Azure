using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging.Abstractions;
using Muflone.Core;
using Muflone.Messages.Commands;
using Muflone.Transport.Azure.Extensions;
using Muflone.Transport.Azure.Factories;
using Muflone.Transport.Azure.Models;

namespace Muflone.Transport.Azure.Tests;

public class ServiceBusTests
{
    [Fact]
    public async Task Can_SendAndReceiveCommand_To_AzureServiceBus()
    {
        var connectionString =
            "Endpoint=sb://brewup.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=1Iy45wRMiuVRD6A/hTYh3dH8Lgn3K/AHxkUMt5QbdOA=";
        var serviceBusSenderFactory = new ServiceBusSenderFactory(new AzureQueueReferenceFactory(),
            new ServiceBusClient(connectionString));
        var serviceBus = new ServiceBus(serviceBusSenderFactory, new NullLogger<ServiceBus>());

        await ServiceBusAdministrator.CreateQueueIfNotExistAsync(new AzureQueueReferences("addorder", "addorder-subscription",
            connectionString));
        await serviceBus.SendAsync(new AddOrder(new OrderId(Guid.NewGuid())));

        Thread.Sleep(1000);
        var messageProcessor = new MessageProcessor();
        var messageSerializer = new MessageSerializer();

        var serviceBusSubscriber = new ServiceBusSubscriber<AddOrder>(new ServiceBusClient(connectionString), messageProcessor,
            messageSerializer, new AzureServiceBusConfiguration(connectionString), new NullLogger<ServiceBusSubscriber<AddOrder>>());

        await serviceBusSubscriber.StartAsync();
        Thread.Sleep(10000);
    }
}

public sealed class AddOrder : Command
{
    public readonly OrderId OrderId;
    public readonly DateTime OrderDate;

    public AddOrder(OrderId aggregateId) : base(aggregateId)
    {
        OrderId = aggregateId;
        OrderDate = DateTime.UtcNow;
    }
}

public class OrderId : DomainId
{
    public OrderId(Guid value) : base(value)
    {
    }
}