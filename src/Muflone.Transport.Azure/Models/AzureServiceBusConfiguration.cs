namespace Muflone.Transport.Azure.Models;

public record AzureServiceBusConfiguration(string ConnectionString, string TopicName, string SubscriptionName = "", int MaxConcurrentCalls = 1);