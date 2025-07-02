namespace EventSourcing.Tests.Stubs;

public class StubDateTimeProvider : IDateTimeProvider
{
    public DateTime StubUtcDateTime { get; }
    public DateTime UtcNow { get; } 

    public StubDateTimeProvider()
    {
        StubUtcDateTime = new DateTime(2030, 10, 23, 12, 0, 0, DateTimeKind.Utc);
        UtcNow = StubUtcDateTime;
    }
}