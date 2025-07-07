namespace EventSourcing.Tests.Stubs;

public class StubAggregate
{
    public Guid Id { get; private set; }
    public string? CustomProperty1 { get; private set; }
    public string? CustomProperty2 { get; private set; }
    public NestedObject? NestedObject { get; private set; }

    public StubAggregate(Guid? id)
    {
        Id = id ?? Guid.NewGuid();
    }

    public StubAggregate()
    {
        
    }
    public void Apply(StubEventOne @event)
    {
        CustomProperty1 = @event.CustomProperty1;
        CustomProperty2 = @event.CustomProperty2;
        NestedObject = new NestedObject
        {
            NestedProperty1 = @event.NestedObject.NestedProperty1,
            NestedProperty2 = @event.NestedObject.NestedProperty2
        };
    }
    public void Apply(StubEventTwo @event)
    {
        CustomProperty2 = @event.CustomProperty2;
    }
}

public class NestedObject
{
    public string NestedProperty1 { get; init; }
    public string NestedProperty2 { get; init; }
}