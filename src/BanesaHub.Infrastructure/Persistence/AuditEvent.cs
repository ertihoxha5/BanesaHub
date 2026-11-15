namespace BanesaHub.Infrastructure.Persistence;

public sealed class AuditEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid? ActorId { get; init; }
    public Guid? OrganizationId { get; init; }
    public required string Action { get; init; }
    public DateTimeOffset At { get; init; }
}
