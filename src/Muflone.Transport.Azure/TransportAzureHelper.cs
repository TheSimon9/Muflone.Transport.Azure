using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Muflone.Messages;
using Muflone.Messages.Commands;
using Muflone.Transport.Azure.Extensions;
using Muflone.Transport.Azure.Factories;
using Muflone.Transport.Azure.Models;

namespace Muflone.Transport.Azure;

public static class TransportAzureHelper
{
    public static IServiceCollection AddMufloneTransportAzure(this IServiceCollection services,
        AzureServiceBusConfiguration azureServiceBusConfiguration,
        IEnumerable<MessageReference> messageReferences)
    {
        var serviceProvider = services.BuildServiceProvider();

        foreach (var message in messageReferences)
        {
            if (string.IsNullOrEmpty(message.SubscriptionName))
            {
                ServiceBusAdministrator.CreateQueueIfNotExistAsync(new AzureQueueReferences(message.GetType().Name, "",
                    azureServiceBusConfiguration.ConnectionString)).GetAwaiter().GetResult();

                //var type = message.GetType();
                //var commandProcessor = new CommandProcessor<type>(
                //    new ServiceBusClient(azureServiceBusConfiguration.ConnectionString),
                //    null,
                //    new MessageSerializer(),
                //    azureServiceBusConfiguration, new NullLogger<CommandProcessor<type>>());

                //commandProcessor.StartAsync().GetAwaiter().GetResult();
            }
            else
            {
                ServiceBusAdministrator.CreateTopicIfNotExistAsync(new AzureQueueReferences(message.GetType().Name,
                    message.SubscriptionName, azureServiceBusConfiguration.ConnectionString)).GetAwaiter().GetResult();
            }
        }
        

        return services;
    }
}