using Muflone.Messages;
using Muflone.Transport.Azure.Abstracts;

namespace Muflone.Transport.Azure;

public class MessageProcessor : IMessageProcessor
{
    public Task ProcessAsync<T>(T message, CancellationToken cancellationToken = default) where T : IMessage
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        return ProcessAsyncCore(message, cancellationToken);
    }

    private async Task ProcessAsyncCore<T>(T message, CancellationToken cancellationToken) where T : IMessage
    {}
}