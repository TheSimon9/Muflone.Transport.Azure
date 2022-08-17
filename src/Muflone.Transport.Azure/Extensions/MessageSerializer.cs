using System.Text.Json;

namespace Muflone.Transport.Azure.Extensions;

public class MessageSerializer : IMessageSerializer
{
    private static readonly JsonSerializerOptions Settings = new()
    {
        IgnoreReadOnlyFields = false,
        IgnoreReadOnlyProperties = false,
        IncludeFields = true,
        PropertyNameCaseInsensitive = true
    };

    public string SerializeAsString<T>(T data)
    {
        var type = data.GetType();
        return JsonSerializer.Serialize(data, type, Settings);
    }

    public byte[] Serialize<T>(T data)
    {
        var type = data.GetType();
        return JsonSerializer.SerializeToUtf8Bytes(data, type, Settings);
    }

    public ValueTask<T> DeserializeAsync<T>(Stream data, CancellationToken cancellationToken = default)
        => JsonSerializer.DeserializeAsync<T>(data, options: Settings);

    public T Deserialize<T>(byte[] data)
        => JsonSerializer.Deserialize<T>(data, options: Settings);

    public object Deserialize(byte[] data, Type type)
        => JsonSerializer.Deserialize(data, type, options: Settings);
}