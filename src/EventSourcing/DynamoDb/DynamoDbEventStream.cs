using System.Reflection;
using System.Text.Json;
using Amazon.DynamoDBv2;

namespace EventSourcing.DynamoDb;

public class DynamoDbEventStream<T> : IEventStream<T>
{
    public Guid Id { get; }

    public T Aggregate { get; private set; } 

    public long StartingVersion { get; }

    public long CurrentVersion { get; }

    
    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private List<EventItem> _events;

    public DynamoDbEventStream(IAmazonDynamoDB dynamoDb, JsonSerializerOptions jsonSerializerOptions,  Guid id, IEnumerable<EventItem> events)
    {
        _events = events.ToList();
        _jsonSerializerOptions = jsonSerializerOptions;
        Id = id;
        StartingVersion = _events.First().Version;
        CurrentVersion = _events.Last().Version;
        _dynamoDb = dynamoDb;
        ApplyEventsToAggregate(_events);
    }
    
    public async Task Append(params object[] events)
    {
        // Implementation for appending events to the stream
        // This would typically involve writing to DynamoDB
        throw new NotImplementedException();
    }
    
    private object DeserializeEvent(string eventData, Type eventType)
    {
        return JsonSerializer.Deserialize(eventData, eventType, _jsonSerializerOptions)!;
    }
    
    private T ApplyEventsToAggregate(IEnumerable<EventItem> events)
    {
        if (Aggregate != null) return Aggregate;

        var aggregateType = typeof(T);
        Aggregate = (T)Activator.CreateInstance(typeof(T));
        var idProperty = aggregateType.GetProperty("Id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (idProperty != null && idProperty.CanWrite)
        {
            idProperty.SetValue(Aggregate, Id);
        }

        foreach (var eventItem in events)
        {
            var eventType = Type.GetType(eventItem.Event);
            var @event = DeserializeEvent(eventItem.Data, eventType!);
            if (@event is null) continue;
            
            var method = aggregateType.GetMethod("Apply", [eventType!]);
            if (method == null) continue;
                
            method.Invoke(Aggregate, [@event]);
        }

        return Aggregate;
    }
}