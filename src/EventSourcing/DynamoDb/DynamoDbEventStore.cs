using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Options;

namespace EventSourcing.DynamoDb;

public class DynamoDbEventStore(IAmazonDynamoDB dynamoDb, IOptions<DynamoDbConfig> dbConfigOptions, IDateTimeProvider dateTimeProvider) : IEventStore
{
    private readonly DynamoDbConfig _dbConfig = dbConfigOptions.Value;
    private readonly static JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions{ PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    
    public async Task StartStream<TAggregate>(Guid id, params object[] events)
    {
        var @event = events.First();
        
        var putItemRequest = new PutItemRequest
        {
            TableName = _dbConfig.EventTableName,
            Item = new Dictionary<string, AttributeValue>
            {
                { "id", new AttributeValue { S = id.ToString() } },
                { "version", new AttributeValue { N = 1.ToString() } }, //we can enhance to support initial versioning later
                { "data", new AttributeValue { S = JsonSerializer.Serialize(@event, _jsonSerializerOptions) } },
                { "event", new AttributeValue { S = @event.GetType().Name } },
                { "timestamp", new AttributeValue { S = dateTimeProvider.UtcNow.ToString("o") } }
            }
        };
        
        var response = await dynamoDb.PutItemAsync(putItemRequest);
    }

    public Task Append(Guid stream, IEnumerable<object> events)
    {
        throw new NotImplementedException();
    }

    public Task FetchLatest<TAggregate>(Guid id)
    {
        throw new NotImplementedException();
    }
}