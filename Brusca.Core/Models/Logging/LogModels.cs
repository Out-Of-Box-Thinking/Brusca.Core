namespace Brusca.Core.Models.Logging;

/// <summary>A structured audit log entry. Written via audit.Log table.</summary>
public sealed class AuditLogEntry
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string EventType { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? Action { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IpAddress { get; set; }
    public string? AdditionalData { get; set; }
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>A structured error log entry. Written via error.Log table.</summary>
public sealed class ErrorLogEntry
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ExceptionType { get; set; }
    public string? ExceptionMessage { get; set; }
    public string? StackTrace { get; set; }
    public string? Source { get; set; }
    public string? CorrelationId { get; set; }
    public string? UserId { get; set; }
    public Guid? CleaningId { get; set; }
    public string? AdditionalData { get; set; }
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
}
