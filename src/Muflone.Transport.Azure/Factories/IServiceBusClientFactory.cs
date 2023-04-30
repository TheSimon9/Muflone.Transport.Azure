using Azure.Messaging.ServiceBus;
using Muflone.Messages;

namespace Muflone.Transport.Azure.Factories;

public interface IServiceBusClientFactory
{
	ServiceBusProcessor CreateProcessor(string topicName);
	
	void CreateQueueIfNotExists(string queueName);
}