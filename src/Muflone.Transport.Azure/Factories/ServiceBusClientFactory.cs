using Azure.Messaging.ServiceBus;
using Muflone.Transport.Azure.Models;

namespace Muflone.Transport.Azure.Factories;

public class ServiceBusClientFactory : IServiceBusClientFactory
{
    private readonly AzureServiceBusConfiguration _configuration;
    private readonly ServiceBusClient _serviceBusClient;

    public ServiceBusClientFactory(AzureServiceBusConfiguration configuration)
    {
        _configuration = configuration;
        _serviceBusClient = new ServiceBusClient(configuration.ConnectionString);
    }
    
    public ServiceBusProcessor CreateProcessor(string topicName)
    {
        return _serviceBusClient.CreateProcessor(topicName,
            subscriptionName: "", new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = false,
                MaxConcurrentCalls = _configuration.MaxConcurrentCalls
            });
    }

    public void CreateQueueIfNotExists(string queueName)
    {
        ServiceBusAdministrator.CreateQueueIfNotExistAsync(new AzureQueueReferences(queueName, "",
            _configuration.ConnectionString)).GetAwaiter().GetResult();
    }
}