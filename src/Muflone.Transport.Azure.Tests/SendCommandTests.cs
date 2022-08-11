using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Muflone.Core;
using Muflone.Messages.Commands;
using Muflone.Transport.Azure.Factories;
using Muflone.Transport.Azure.Models;

namespace Muflone.Transport.Azure.Tests;

public class SendCommandTests
{
    [Fact]
    public async Task Can_SendCommand_To_AzureServiceBus()
    {
        string connectionString =
            "Endpoint=sb://brewup.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=1Iy45wRMiuVRD6A/hTYh3dH8Lgn3K/AHxkUMt5QbdOA=";
        var clientId = Guid.NewGuid();
        var clientInfo = new ClientInfo("MufloneGroup", clientId, false);
        var servicebusSenderFactory = new ServiceBusSenderFactory(new AzureQueueReferenceFactory(),
            new ServiceBusClient(connectionString));
        var serviceBus = new ServiceBus(servicebusSenderFactory, new NullLogger<ServiceBus>(), clientInfo);

        await ServiceBusAdministrator.CreateQueueIfNotExistAsync(new AzureQueueReferences("addorder", "addorder-subscription",
            connectionString));
        await serviceBus.SendAsync(new AddOrder(new OrderId(Guid.NewGuid())));
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