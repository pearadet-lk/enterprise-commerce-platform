namespace Contracts.Audit;

public record AuditLogDto(
    Guid Id,
    string Action,
    string EntityType,
    string? EntityId,
    string UserId,
    string UserName,
    DateTime Timestamp,
    string? Details);
