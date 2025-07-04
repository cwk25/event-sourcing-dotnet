namespace EventSourcing;

public interface IEventStream<T>
{
    Guid Id { get; }
    
    T Aggregate { get; }
    
    long StartingVersion { get; }
    
    long CurrentVersion { get; }

    Task Append(params object[] events);
}