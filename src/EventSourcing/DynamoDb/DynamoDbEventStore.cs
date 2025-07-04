using System.Net;
using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using EventSourcing.Exceptions;
using Microsoft.Extensions.Options;

namespace EventSourcing.DynamoDb;

public class DynamoDbEventStore(IAmazonDynamoDB dynamoDb, IOptions<DynamoDbConfig> dbConfigOptions, IDateTimeProvider dateTimeProvider) : IEventStore
{
    private readonly DynamoDbConfig _dbConfig = dbConfigOptions.Value;
    private readonly static JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    
    public async Task StartStream<TAggregate>(Guid id, params object[] events)
    {
        var version = 1;
        while (version <= events.Length)
        {
            var index = version - 1;
            var putItemRequest = new PutItemRequest
            {
                TableName = _dbConfig.EventTableName,
                Item = new Dictionary<string, AttributeValue>
                {
                    { "id", new AttributeValue { S = id.ToString() } },
                    { "version", new AttributeValue { N = version.ToString() } }, //we can enhance to support initial versioning later
                    { "data", new AttributeValue { S = JsonSerializer.Serialize(events[index], _jsonSerializerOptions) } },
                    { "event", new AttributeValue { S = events[index].GetType().Name } },
                    { "timestamp", new AttributeValue { S = dateTimeProvider.UtcNow.ToString("o") } }
                },
                ConditionExpression = "attribute_not_exists(#id) AND attribute_not_exists(#version)",
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    { "#id", "id" },
                    { "#version", "version" }
                }
            };
            try
            {
                var response = await dynamoDb.PutItemAsync(putItemRequest);
                version++;
            }
            catch (ConditionalCheckFailedException e)
            {
                throw new ConcurrencyException(e.Message);
            }
            catch (Exception e)
            {
                throw;
            }
        }
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