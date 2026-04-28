using Brusca.Core.Models.PathAccess;
using FluentResults;

namespace Brusca.Core.Contracts.Repositories;

/// <summary>
/// Persists encrypted-at-rest <see cref="PathCredentialRecord"/> rows that
/// gate access to remote shares for a cleaning. Rows are scoped per
/// <c>(CleaningId, RootPath)</c> and purged when the cleaning is archived.
/// </summary>
public interface IPathCredentialRepository
{
    /// <summary>
    /// Upsert a credential row for <c>(CleaningId, RootPath)</c>. The
    /// existing row, if any, is replaced — credentials are not versioned.
    /// </summary>
    Task<Result> SaveAsync(
        PathCredentialRecord record, CancellationToken ct = default);

    /// <summary>Returns the saved credential for the cleaning + path, or null.</summary>
    Task<Result<PathCredentialRecord?>> GetAsync(
        Guid cleaningId, string rootPath, CancellationToken ct = default);

    /// <summary>Deletes every credential row for the cleaning.</summary>
    Task<Result> DeleteByCleaningIdAsync(
        Guid cleaningId, CancellationToken ct = default);
}
