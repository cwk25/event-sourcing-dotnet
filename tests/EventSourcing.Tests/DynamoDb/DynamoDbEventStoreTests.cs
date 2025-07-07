using System.Globalization;
using System.Text.Json;
using Amazon.DynamoDBv2.Model;
using EventSourcing.DynamoDb;
using EventSourcing.Exceptions;
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
    private readonly IOptions<DynamoDbConfig> _dbConfigOptions = Substitute.For<IOptions<DynamoDbConfig>>();
    

    public DynamoDbEventStoreTests(DynamoDbFixture fixture)
    {
        _fixture = fixture;
        _dbConfigOptions.Value.Returns(new DynamoDbConfig{ EventTableName = _fixture.TableName });
        var eventSerializer = new StubEventSerializer();
        _sut = new DynamoDbEventStore(_fixture.DDbClient, _dbConfigOptions, _dateTimeProvider);
    }
    
    [Fact]
    public async Task StartStream_WhenInvoked_ShouldAddItemToDynamoDb()
    {
        var id = Guid.NewGuid();
        var @event = new StubEventOne(
            "prop1", 
            "prop2", 
            new NestedObject{ NestedProperty1 = "nestedValue1", NestedProperty2 = "nestedValue2" });
        
        await _sut.StartStream<StubAggregate>(id, @event);

        var getItemRequest = new GetItemRequest
        {
            TableName = _fixture.TableName,
            Key = new Dictionary<string, AttributeValue>
            {
                { "id", new AttributeValue { S = id.ToString() } },
                { "version", new AttributeValue{ N = "1" } }
            }
        };
        
        var result = await _fixture.DDbClient.GetItemAsync(getItemRequest);
        
        result.Item.Should().BeEquivalentTo(new Dictionary<string, AttributeValue>
        {
            { "id", new AttributeValue { S = id.ToString() } },
            { "version", new AttributeValue { N = "1" } },
            { "data", new AttributeValue { S = "{\"customProperty1\":\"prop1\",\"customProperty2\":\"prop2\",\"nestedObject\":{\"nestedProperty1\":\"nestedValue1\",\"nestedProperty2\":\"nestedValue2\"}}" } },
            { "event", new AttributeValue { S = "EventSourcing.Tests.Stubs.StubEventOne, EventSourcing.Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null" } },
            { "timestamp", new AttributeValue { S = _dateTimeProvider.UtcNow.ToString("o") } }
        }, options => options.ExcludingMissingMembers());
    }
    
    [Fact]
    public async Task StartStream_WhenContainsMultipleEvents_ShouldAddItemsInSequenceToDynamoDb()
    {
        var id = Guid.NewGuid();
        var @event = new StubEventOne(
            "prop1", 
            "prop2", 
            new NestedObject{ NestedProperty1 = "nestedValue1", NestedProperty2 = "nestedValue2" });
        var event2 = new StubEventTwo("prop2Modified");
        
        await _sut.StartStream<StubAggregate>(id, @event, event2);

        var queryRequest = new QueryRequest
        {
            TableName = _fixture.TableName,
            KeyConditionExpression = "id = :id",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":id", new AttributeValue { S = id.ToString() } }
            },
            ScanIndexForward = false // to get the latest version first
        };
        var result = await _fixture.DDbClient.QueryAsync(queryRequest);
        
        result.Items.Should().BeEquivalentTo([
                new Dictionary<string, AttributeValue>
                {
                    { "id", new AttributeValue { S = id.ToString() } },
                    { "version", new AttributeValue { N = "1" } },
                    { "data", new AttributeValue { S = "{\"customProperty1\":\"prop1\",\"customProperty2\":\"prop2\",\"nestedObject\":{\"nestedProperty1\":\"nestedValue1\",\"nestedProperty2\":\"nestedValue2\"}}" } },
                    { "event", new AttributeValue { S =  "EventSourcing.Tests.Stubs.StubEventOne, EventSourcing.Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null" } },
                    { "timestamp", new AttributeValue { S = _dateTimeProvider.UtcNow.ToString("o") } }
                },
                new Dictionary<string, AttributeValue>
                {
                    { "id", new AttributeValue { S = id.ToString() } },
                    { "version", new AttributeValue { N = "2" } },
                    { "data", new AttributeValue { S = "{\"customProperty2\":\"prop2Modified\"}" } },
                    { "event", new AttributeValue { S = "EventSourcing.Tests.Stubs.StubEventTwo, EventSourcing.Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null" } },
                    { "timestamp", new AttributeValue { S = _dateTimeProvider.UtcNow.ToString("o") } }
                },
            ], options => options.ExcludingMissingMembers());
    }
    
    [Fact]
    public async Task StartStream_WhenItemAlreadyExists_ShouldThrowConcurrencyException()
    {
        var id = Guid.NewGuid();
        var @event = new StubEventOne(
            "prop1", 
            "prop2", 
            new NestedObject{ NestedProperty1 = "nestedValue1", NestedProperty2 = "nestedValue2" });

        await _sut.StartStream<StubAggregate>(id, @event);
        

        var result = async () => await _sut.StartStream<StubAggregate>(id, @event);
        
        await result.Should().ThrowAsync<ConcurrencyException>();
    }
    
    [Fact]
    public async Task Fetch_WhenInvoked_ShouldReturnEventStream()
    {
        var id = Guid.NewGuid();
        var events = CreateEvents();
        
        await _sut.StartStream<StubAggregate>(id, events);

        var stream = await _sut.Fetch<StubAggregate>(id);

        stream.Should().BeEquivalentTo(
            new DynamoDbEventStream<StubAggregate>(
                _fixture.DDbClient,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase },
                id,
                [
                    new EventItem(id,
                        1,
                        "{\"customProperty1\":\"prop1\",\"customProperty2\":\"prop2\",\"nestedObject\":{\"nestedProperty1\":\"nestedValue1\",\"nestedProperty2\":\"nestedValue2\"}}",
                        "EventSourcing.Tests.Stubs.StubEventOne, EventSourcing.Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                        DateTime.Parse(_dateTimeProvider.UtcNow.ToString("o"), null, DateTimeStyles.RoundtripKind)),
                    new EventItem(id,
                        2,
                        "{\"customProperty2\":\"prop2Modified\"}",
                        "EventSourcing.Tests.Stubs.StubEventTwo, EventSourcing.Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                        DateTime.Parse(_dateTimeProvider.UtcNow.ToString("o"), null, DateTimeStyles.RoundtripKind))
                ]));
    }
    
    private static object[] CreateEvents()
    {
        return new object[]
        {
            new StubEventOne("prop1", "prop2", new NestedObject { NestedProperty1 = "nestedValue1", NestedProperty2 = "nestedValue2" }),
            new StubEventTwo("prop2Modified")
        };
    }
}