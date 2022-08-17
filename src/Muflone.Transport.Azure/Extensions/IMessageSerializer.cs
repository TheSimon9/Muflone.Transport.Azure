namespace Muflone.Transport.Azure.Extensions;

public interface IMessageSerializer
{
    string SerializeAsString<T>(T data);
    byte[] Serialize<T>(T data);
    ValueTask<T> DeserializeAsync<T>(Stream data, CancellationToken cancellationToken = default);
    object Deserialize(byte[] data, Type type);
    T Deserialize<T>(byte[] data);
}