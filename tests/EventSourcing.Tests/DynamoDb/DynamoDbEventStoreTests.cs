using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using EventSourcing.DynamoDb;
using EventSourcing.Tests.Stubs;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace EventSourcing.Tests.DynamoDb;

public class DynamoDbEventStoreTests : IClassFixture<DynamoDbFixture>
{
    private readonly DynamoDbFixture _fixture;
    private readonly DynamoDbEventStore _sut;
    private readonly StubDateTimeProvider _dateTimeProvider = new ();
    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly IOptions<DynamoDbConfig> _dbConfigOptions = Substitute.For<IOptions<DynamoDbConfig>>();
    

    public DynamoDbEventStoreTests(DynamoDbFixture fixture)
    {
        _fixture = fixture;
        _dynamoDb = _fixture.DDbClient;
        _dbConfigOptions.Value.Returns(new DynamoDbConfig{ EventTableName = _fixture.TableName });
        _sut = new DynamoDbEventStore(_dynamoDb, _dbConfigOptions, _dateTimeProvider);
    }
    
    [Fact]
    public async Task AddAsync_WhenInvoked_ShouldAddItemToDynamoDb()
    {
        var id = Guid.NewGuid();
        var @event = new StubEventOne(
            "prop1", 
            "prop2", 
            new NestedObject{ NestedProperty1 = "nestedValue1", NestedProperty2 = "nestedValue2" });
        
        await _sut.StartStream<StubEventOne>(id, @event);

        var getItemRequest = new GetItemRequest
        {
            TableName = _fixture.TableName,
            Key = new Dictionary<string, AttributeValue>
            {
                { "id", new AttributeValue { S = id.ToString() } },
                { "version", new AttributeValue { N = "1" } }
            }
        };
        var result = await _fixture.DDbClient.GetItemAsync(getItemRequest);
        
        result.Item.Should().BeEquivalentTo(new Dictionary<string, AttributeValue>
        {
            { "id", new AttributeValue { S = id.ToString() } },
            { "version", new AttributeValue { N = "1" } },
            { "data", new AttributeValue { S = "{\"customProperty1\":\"prop1\",\"customProperty2\":\"prop2\",\"nestedObject\":{\"nestedProperty1\":\"nestedValue1\",\"nestedProperty2\":\"nestedValue2\"}}" } },
            { "event", new AttributeValue { S = nameof(StubEventOne) } },
            { "timestamp", new AttributeValue { S = _dateTimeProvider.UtcNow.ToString("o") } }
        }, options => options.ExcludingMissingMembers());
    }
}