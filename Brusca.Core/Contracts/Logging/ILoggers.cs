using Brusca.Core.Models.Logging;

namespace Brusca.Core.Contracts.Logging;

/// <summary>
/// Audit logger — logs WHO did WHAT to WHICH entity.
/// Configured in appsettings.json: Logging:Audit:Sink = Database | File | Both | Elasticsearch
/// </summary>
public interface IAuditLogger
{
    Task LogAsync(AuditLogEntry entry, CancellationToken ct = default);
    Task LogAsync(string eventType, string entityType, string? entityId = null,
                  string? userId = null, string? action = null,
                  object? oldValues = null, object? newValues = null,
                  CancellationToken ct = default);
}

/// <summary>
/// Error logger — logs application exceptions and operational issues.
/// Configured in appsettings.json: Logging:Error:Sink = Database | File | Both | Elasticsearch
/// Minimum level: Logging:Error:MinimumLevel
/// </summary>
public interface IErrorLogger
{
    Task LogErrorAsync(string message, Exception? ex = null, string? correlationId = null,
                       Guid? cleaningId = null, string? userId = null, CancellationToken ct = default);
    Task LogWarningAsync(string message, string? correlationId = null, CancellationToken ct = default);
    Task LogInformationAsync(string message, string? correlationId = null, CancellationToken ct = default);
    Task LogCriticalAsync(string message, Exception? ex = null, string? correlationId = null, CancellationToken ct = default);
}
