using Azure.Messaging.ServiceBus;
using Muflone.Messages;

namespace Muflone.Transport.Azure.Factories;

public interface IServiceBusSenderFactory
{
	ServiceBusSender Create<T>(T message) where T : IMessage;
}