using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Muflone.Core;
using Muflone.Factories;
using Muflone.Messages.Commands;
using Muflone.Transport.Azure.Abstracts;
using Muflone.Transport.Azure.Consumers;
using Muflone.Transport.Azure.Extensions;
using Muflone.Transport.Azure.Factories;
using Muflone.Transport.Azure.Models;

namespace Muflone.Transport.Azure.Tests;

public class ServiceBusTests
{
    private readonly IServiceProvider _serviceProvider;

    public ServiceBusTests()
    {
        var services = new ServiceCollection();

        services.AddScoped<ICommandHandlerAsync<AddOrder>, AddOrderCommandHandler<AddOrder>>();
        services.AddScoped<ICommandHandlerFactoryAsync, CommandHandlerFactoryAsync>();

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public void Can_Serialize_And_Deserialize_Command()
    {
        var serializer = new MessageSerializer();
        var command = new AddOrder(new OrderId(Guid.NewGuid()), DateTime.UtcNow);
        var serializedCommand = serializer.Serialize(command );
        var commandDeserialize = serializer.Deserialize<AddOrder>(serializedCommand);

        Assert.Equal(command.OrderId, commandDeserialize.OrderId);
    }

    [Fact]
    public async Task Can_SendAndReceiveCommand_With_AzureServiceBus()
    {
        var connectionString =
            "Endpoint=sb://brewup.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=1Iy45wRMiuVRD6A/hTYh3dH8Lgn3K/AHxkUMt5QbdOA=";

        var configurations = new List<AzureServiceBusConfiguration>
        {
            new (connectionString, nameof(AddOrder), string.Empty)
        };
        var serviceBusSenderFactory = new ServiceBusSenderFactory(new ServiceBusClient(connectionString), configurations);
        var serviceBus = new ServiceBus(serviceBusSenderFactory, new NullLogger<ServiceBus>());

        await ServiceBusAdministrator.CreateQueueIfNotExistAsync(new AzureQueueReferences("addorder", "addorder-subscription",
            connectionString));
        var command = new AddOrder(new OrderId(Guid.NewGuid()), DateTime.UtcNow);
        await serviceBus.SendAsync(command);

        var commandProcessor = new AddOrderProcessor(new AzureServiceBusConfiguration(connectionString, "addorder", "ServiceBusTest"), 
            new NullLoggerFactory());

        await commandProcessor.StartAsync();
        Thread.Sleep(10000);
    }
}

public class AddOrderProcessor : CommandConsumerBase<AddOrder>
{
    public AddOrderProcessor(AzureServiceBusConfiguration azureServiceBusConfiguration, ILoggerFactory loggerFactory,
        IMessageSerializer? messageSerializer = null) : base(azureServiceBusConfiguration, loggerFactory,
        messageSerializer)
    {
        CommandHandlerAsync = new AddOrderCommandHandler<AddOrder>();
    }

    protected override ICommandHandlerAsync<AddOrder> CommandHandlerAsync { get; }
}

//public class AddOrderConsumer : CommandConsumerBase<AddOrder>
//{
//    public ICommandHandlerAsync<AddOrder> CommandHandlerAsync = new AddOrderCommandHandler<AddOrder>();
//}

public record AzureCommand(Guid OrderId, DateTime OrderDate)
{
    //public static AzureCommand New() => new(Guid.NewGuid(), DateTime.UtcNow);
}

public class AddOrder: Command
{
    public readonly OrderId OrderId;
    public readonly DateTime OrderDate;

    public AddOrder(OrderId aggregateId, DateTime orderDate) : base(aggregateId)
    {
        OrderId = aggregateId;
        OrderDate = orderDate;
    }
}

public class OrderId : DomainId
{
    public OrderId(Guid value) : base(value)
    {
    }
}

public class AddOrderCommandHandler<AddOrder> : ICommandHandlerAsync<AddOrder> where AddOrder : class, ICommand
{
    public Task HandleAsync(AddOrder command, CancellationToken cancellationToken = new())
    {
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        
    }
}

public class CommandHandlerFactoryAsync : ICommandHandlerFactoryAsync
{
    private readonly IServiceProvider _serviceProvider;

    public CommandHandlerFactoryAsync(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ICommandHandlerAsync<T> CreateCommandHandlerAsync<T>() where T : class, ICommand
    {
        return _serviceProvider.GetService<ICommandHandlerAsync<T>>()!;
    }
}