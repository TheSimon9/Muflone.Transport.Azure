using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Muflone.Core;
using Muflone.Factories;
using Muflone.Messages.Commands;
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
    public async Task Can_SendAndReceiveCommand_To_AzureServiceBus()
    {
        var connectionString =
            "Endpoint=sb://brewup.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=1Iy45wRMiuVRD6A/hTYh3dH8Lgn3K/AHxkUMt5QbdOA=";
        
        var serviceBusSenderFactory = new ServiceBusSenderFactory(new AzureQueueReferenceFactory(),
            new ServiceBusClient(connectionString));
        var serviceBus = new ServiceBus(serviceBusSenderFactory, new NullLogger<ServiceBus>());

        await ServiceBusAdministrator.CreateQueueIfNotExistAsync(new AzureQueueReferences("addorder", "addorder-subscription",
            connectionString));
        var command = new AddOrder(new OrderId(Guid.NewGuid()), DateTime.UtcNow);
        await serviceBus.SendAsync(command);

        Thread.Sleep(1000);
        var commandProcessor = new CommandProcessor<AddOrder>(new ServiceBusClient(connectionString),
            new CommandHandlerFactoryAsync(_serviceProvider), new MessageSerializer(),
            new AzureServiceBusConfiguration(connectionString), new NullLogger<CommandProcessor<AddOrder>>());

        await commandProcessor.StartAsync();
        Thread.Sleep(10000);
    }
}

public record AzureCommand(Guid OrderId, DateTime OrderDate)
{
    //public static AzureCommand New() => new(Guid.NewGuid(), DateTime.UtcNow);
}

public record AddOrder(OrderId OrderId, DateTime OrderDate) : ICommand
{
    public Guid MessageId { get; set; } = Guid.NewGuid();
    public Dictionary<string, object> UserProperties { get; set; } = new();
    public DomainId AggregateId { get; } = OrderId;
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