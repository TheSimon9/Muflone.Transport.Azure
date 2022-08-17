using Muflone.Messages;

namespace Muflone.Transport.Azure.Models;

public record MessageReference(IMessage Message, string SubscriptionName);