using System.Text.Json;

namespace EventSourcing.Tests.Stubs;

public class StubEventSerializer : IEventSerializer
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    
    public string Serialize(object obj)
    {
        return JsonSerializer.Serialize(obj, _jsonSerializerOptions);
    }

    public T Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, _jsonSerializerOptions);
    }

    public object Deserialize(string json, Type type)
    {
        return JsonSerializer.Deserialize(json, type, _jsonSerializerOptions);
    }
}