using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Muflone.Persistence;
using Muflone.Transport.Azure.Abstracts;
using Muflone.Transport.Azure.Factories;
using Muflone.Transport.Azure.Models;

namespace Muflone.Transport.Azure.Tests;

public class TransportAzureHelperTests
{
    private readonly Mock<AzureServiceBusConfiguration> _azureServiceBusConfigurationMock;
    public TransportAzureHelperTests()
    {
        _azureServiceBusConfigurationMock = new Mock<AzureServiceBusConfiguration>(
            new AzureServiceBusConfiguration("Endpoint=sb://my-service-bus.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX=", 
                "fakeTopic",
            "fakeServiceBusClientId"));
    }
    
    [Fact]
    public void AddMufloneTransportAzure_AddsServices()
    {
        var serviceCollection = new ServiceCollection();

        var fakeConsumer = new Mock<FakeConsumer>();
        
        fakeConsumer.Setup(x=>x.StartAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        fakeConsumer.Setup(x=>x.StopAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        
        var consumers = new List<IConsumer> { fakeConsumer.Object };
        
        var res = serviceCollection.AddMufloneTransportAzure(_azureServiceBusConfigurationMock.Object, consumers);
        
        Assert.Contains(res, x => x.ServiceType == typeof(IEnumerable<AzureServiceBusConfiguration>));
        Assert.Contains(res, x => x.ServiceType == typeof(ServiceBusClient));
        Assert.Contains(res, x => x.ServiceType == typeof(IServiceBusSenderFactory));
        Assert.Contains(res, x => x.ServiceType == typeof(IServiceBus));
        Assert.Contains(res, x => x.ServiceType == typeof(IEventBus));
    }


    public class FakeConsumer : IConsumer
    {
        public string TopicName => "fakeTopic";
        public virtual Task StartAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public virtual Task StopAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}