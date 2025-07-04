namespace EventSourcing;

public record EventItem(Guid Id, long Version, string Data, string Event, DateTime Timestamp);