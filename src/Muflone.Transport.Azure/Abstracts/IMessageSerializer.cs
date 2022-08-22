namespace Muflone.Transport.Azure.Abstracts;

public interface IMessageSerializer
{
    byte[] Serialize<T>(T data);
    T Deserialize<T>(byte[] data);
}