namespace EventSourcing.Tests.Stubs;

public class StubAggregate
{
    public Guid Id { get; private set; }
    public string CustomProperty1 { get; private set; }
    public string CustomProperty2 { get; private set; }
    public NestedObject NestedObject { get; private set; }
}

public class NestedObject
{
    public string NestedProperty1 { get; init; }
    public string NestedProperty2 { get; init; }
}