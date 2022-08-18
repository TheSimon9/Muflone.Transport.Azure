﻿using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Muflone.Transport.Azure.Abstracts;
using Muflone.Transport.Azure.Factories;
using Muflone.Transport.Azure.Models;

namespace Muflone.Transport.Azure;

public static class TransportAzureHelper
{
    public static IServiceCollection AddMufloneTransportAzure(this IServiceCollection services,
        AzureServiceBusConfiguration azureServiceBusConfiguration,
        IEnumerable<IConsumer> messageConsumers)
    {
        foreach (var consumer in messageConsumers)
        {
            consumer.StartAsync().GetAwaiter().GetResult();
        }

        services.AddSingleton(new ServiceBusClient(azureServiceBusConfiguration.ConnectionString));
        services.AddSingleton<IServiceBusSenderFactory, ServiceBusSenderFactory>();
        services.AddSingleton<IServiceBus, ServiceBus>();
        services.AddSingleton<IEventBus, ServiceBus>();

        return services;
    }
}