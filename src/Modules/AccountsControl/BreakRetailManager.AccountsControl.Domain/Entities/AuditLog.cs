namespace BreakRetailManager.AccountsControl.Domain.Entities;

public sealed class AuditLog
{
    private AuditLog()
    {
    }

    public AuditLog(
        string action,
        string entityType,
        string entityId,
        string performedBy,
        DateTimeOffset performedAt,
        string? payloadJson = null)
    {
        if (string.IsNullOrWhiteSpace(action))
        {
            throw new ArgumentException("Action is required.", nameof(action));
        }

        if (string.IsNullOrWhiteSpace(entityType))
        {
            throw new ArgumentException("Entity type is required.", nameof(entityType));
        }

        if (string.IsNullOrWhiteSpace(entityId))
        {
            throw new ArgumentException("Entity ID is required.", nameof(entityId));
        }

        if (string.IsNullOrWhiteSpace(performedBy))
        {
            throw new ArgumentException("PerformedBy is required.", nameof(performedBy));
        }

        Id = Guid.NewGuid();
        Action = action.Trim();
        EntityType = entityType.Trim();
        EntityId = entityId.Trim();
        PerformedBy = performedBy.Trim();
        PerformedAt = performedAt;
        PayloadJson = string.IsNullOrWhiteSpace(payloadJson) ? null : payloadJson;
    }

    public Guid Id { get; private set; }

    public string Action { get; private set; } = string.Empty;

    public string EntityType { get; private set; } = string.Empty;

    public string EntityId { get; private set; } = string.Empty;

    public string PerformedBy { get; private set; } = string.Empty;

    public DateTimeOffset PerformedAt { get; private set; }

    public string? PayloadJson { get; private set; }
}
