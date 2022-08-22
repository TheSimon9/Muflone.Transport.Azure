using System.Text;
using Muflone.Transport.Azure.Abstracts;
using Newtonsoft.Json;

namespace Muflone.Transport.Azure.Extensions;

public class MessageSerializer : IMessageSerializer
{
    public byte[] Serialize<T>(T data)
        => Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data));

    public T Deserialize<T>(byte[] data)
        => JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(data))!;
}