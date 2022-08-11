using Azure.Messaging.ServiceBus.Administration;
using Muflone.Transport.Azure.Models;

namespace Muflone.Transport.Azure.Factories;

public static class ServiceBusAdministrator
{
	public static async Task CreateTopicIfNotExistAsync(AzureQueueReferences azureQueueReferences)
	{
		var adminClient = new ServiceBusAdministrationClient(azureQueueReferences.ConnectionString);
		var topicExists = await adminClient.TopicExistsAsync(azureQueueReferences.TopicName);

		if (!topicExists)
		{
			var options = new CreateTopicOptions(azureQueueReferences.TopicName)
			{
				MaxSizeInMegabytes = 1024
			};
			await adminClient.CreateTopicAsync(options);
		}

		var subscriptionExists =
			await adminClient.SubscriptionExistsAsync(azureQueueReferences.TopicName, azureQueueReferences.SubscriptionName);
		if (!subscriptionExists)
		{
			var options = new CreateSubscriptionOptions(azureQueueReferences.TopicName, azureQueueReferences.SubscriptionName)
			{
				DefaultMessageTimeToLive = new TimeSpan(14, 0, 0, 0),
				DeadLetteringOnMessageExpiration = true,
				EnableDeadLetteringOnFilterEvaluationExceptions = true
			};
			await adminClient.CreateSubscriptionAsync(options);
		}
	}

	public static async Task CreateQueueIfNotExistAsync(AzureQueueReferences azureQueueReferences)
	{
		var adminClient = new ServiceBusAdministrationClient(azureQueueReferences.ConnectionString);
		var queueExists = await adminClient.QueueExistsAsync(azureQueueReferences.TopicName);

		if (!queueExists)
		{
			var options = new CreateQueueOptions(azureQueueReferences.TopicName)
			{
				MaxDeliveryCount = 10,
				DeadLetteringOnMessageExpiration = true
			};
			await adminClient.CreateQueueAsync(options);
		}
	}
}