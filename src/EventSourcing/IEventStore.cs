namespace EventSourcing;

public interface IEventStore
{
    /// <summary>
    /// Create a new stream for an aggregate
    /// </summary>
    /// <param name="id"></param>
    /// <param name="events"></param>
    /// <typeparam name="TAggregate"></typeparam>
    /// <returns></returns>
    Task StartStream<TAggregate>(Guid id, params object[] events);

    /// <summary>
    /// Append events to an existing stream of an aggregate.
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="events"></param>
    /// <returns></returns>
    Task Append(Guid stream, params object[] events);

    /// <summary>
    /// Fetch the projected aggregate T by id.
    /// </summary>
    /// <param name="id"></param>
    /// <typeparam name="TAggregate"></typeparam>
    /// <returns></returns>
    Task<IEventStream<TAggregate>> Fetch<TAggregate>(Guid id);
}