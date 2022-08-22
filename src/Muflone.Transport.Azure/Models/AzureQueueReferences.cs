namespace Muflone.Transport.Azure.Models;

public record AzureQueueReferences(string TopicName, string SubscriptionName, string ConnectionString);