namespace Muflone.Transport.Azure.Models;

public record AzureServiceBusConfiguration(string ConnectionString, int MaxConcurrentCalls = 1);