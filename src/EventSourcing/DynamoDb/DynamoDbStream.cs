using System.Text.Json;
using Amazon.DynamoDBv2;

namespace EventSourcing.DynamoDb;

public class DynamoDbStream<T> : IEventStream<T>
{
    public Guid Id { get; }
    public T Aggregate { get; }
    public long StartingVersion { get; }

    public long CurrentVersion { get; }

    
    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public DynamoDbStream(IAmazonDynamoDB dynamoDb, Guid id, IEnumerable<EventItem> events, JsonSerializerOptions jsonSerializerOptions)
    {
        Id = id;
        // StartingVersion = startingVersion;
        // CurrentVersion = currentVersion;
        _dynamoDb = dynamoDb;
        _jsonSerializerOptions = jsonSerializerOptions;
    }
    
    public async Task Append(params object[] events)
    {
        // Implementation for appending events to the stream
        // This would typically involve writing to DynamoDB
        throw new NotImplementedException();
    }
}