namespace EventSourcing;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}