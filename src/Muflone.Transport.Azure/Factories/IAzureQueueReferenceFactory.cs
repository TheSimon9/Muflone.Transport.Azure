using Muflone.Messages;
using Muflone.Transport.Azure.Models;

namespace Muflone.Transport.Azure.Factories;

public interface IAzureQueueReferenceFactory
{
    AzureQueueReferences Create<T>() where T : IMessage;
}