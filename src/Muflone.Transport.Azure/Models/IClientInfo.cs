namespace Muflone.Transport.Azure.Models;

public record ClientInfo(string ClientGroup, Guid ClientId, bool PublishOnly);