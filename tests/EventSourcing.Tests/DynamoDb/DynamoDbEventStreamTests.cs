using System.Text.Json;
using EventSourcing.DynamoDb;
using EventSourcing.Tests.Stubs;
using FluentAssertions;

namespace EventSourcing.Tests.DynamoDb;

public class DynamoDbEventStreamTests : IClassFixture<DynamoDbFixture>
{
    private readonly DynamoDbFixture _fixture;
    private readonly DynamoDbEventStream<StubAggregate> _sut;
    private readonly Guid _streamId = Guid.NewGuid();
    
    public DynamoDbEventStreamTests(DynamoDbFixture fixture)
    {
        _fixture = fixture;
        _sut = new DynamoDbEventStream<StubAggregate>(
            _fixture.DDbClient,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase },
            _streamId, 
            FetchEvents());
    }

    [Fact]
    public async Task NewDynamoDbEventStream_WhenCreated_ShouldInitializeAggregateProperties()
    {
        var result = _sut;
        
        result.Id.Should().Be(_streamId);
        result.StartingVersion.Should().Be(1);
        result.CurrentVersion.Should().Be(2);
    }

    [Fact]
    public async Task Aggregate_WhenInitialized_ShouldProjectLatestAggregateState()
    {
        var expectedResult = new StubAggregate(_streamId);
        expectedResult.Apply(new StubEventOne("prop1", "prop2",
            new NestedObject
            {
                NestedProperty1 = "nestedValue1",
                NestedProperty2 = "nestedValue2"
            }));
        expectedResult.Apply(new StubEventTwo("prop2Modified"));
        
        var result = _sut.Aggregate;

        result.Should().BeEquivalentTo(expectedResult);
    }

    private List<EventItem> FetchEvents()
    {
        return new List<EventItem>
        {
            new EventItem(
                _streamId,
                1,
                "{\"customProperty1\":\"prop1\",\"customProperty2\":\"prop2\",\"nestedObject\":{\"nestedProperty1\":\"nestedValue1\",\"nestedProperty2\":\"nestedValue2\"}}",
                typeof(StubEventOne).AssemblyQualifiedName,
                DateTime.UtcNow),
            new EventItem(
                _streamId,
                2,
                "{\"customProperty2\":\"prop2Modified\"}",
                typeof(StubEventTwo).AssemblyQualifiedName,
                DateTime.UtcNow)
        };
    }
}