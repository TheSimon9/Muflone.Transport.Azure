namespace Muflone.Transport.Azure.Models;

public record AzureServiceBusConfiguration(string ConnectionString, string TopicName, string ClientId, int MaxConcurrentCalls = 1);