using Brusca.Core.Models.PathAccess;
using FluentResults;

namespace Brusca.Core.Contracts.Services;

/// <summary>
/// Reachability + credential gating for paths the user typed into the UI.
/// Three classes of path are recognised:
///
///   1. Server-local            — the host can read directly. No credentials.
///   2. Reachable remote share  — UNC/SMB/NFS the server can mount with
///                                stored or just-supplied credentials.
///   3. Client-local            — lives on the user's browser machine. The
///                                server CAN NOT reach this; the UI must
///                                refuse with <c>IsLikelyClientLocal</c>.
///
/// Implementations MUST treat credentials with the same care as PII:
/// encrypt at rest via <c>IEncryptionService</c>, never log values, scope
/// to a single cleaning, purge on archive.
/// </summary>
public interface IPathAccessService
{
    /// <summary>
    /// Non-throwing reachability sniff. Returns a <see cref="PathProbeResult"/>
    /// with flags the UI can map to (proceed / prompt-for-creds / refuse).
    /// </summary>
    Task<Result<PathProbeResult>> ProbeAsync(
        string path, CancellationToken ct = default);

    /// <summary>
    /// Ensures the server can read <paramref name="path"/> for the duration
    /// of the cleaning. For server-local paths returns a no-op handle. For
    /// remote shares looks up any saved <see cref="PathCredentials"/> and
    /// asks the platform-specific mounter to attach.
    /// </summary>
    Task<Result<PathMountHandle>> MountAsync(
        Guid cleaningId, string path, CancellationToken ct = default);

    /// <summary>
    /// Tears down every mount opened for <paramref name="cleaningId"/>.
    /// Called by <c>ArchiveCleaningAsync</c> after credentials are purged.
    /// </summary>
    Task<Result> UnmountAsync(Guid cleaningId, CancellationToken ct = default);

    /// <summary>
    /// Encrypts and persists the supplied credentials against the cleaning.
    /// Subsequent <see cref="MountAsync"/> calls for that cleaning + path
    /// will use them. The cleartext password is never stored.
    /// </summary>
    Task<Result> SaveCredentialsAsync(
        Guid cleaningId, string path, PathCredentials credentials,
        CancellationToken ct = default);

    /// <summary>Removes every stored credential row for the cleaning.</summary>
    Task<Result> PurgeCredentialsAsync(Guid cleaningId, CancellationToken ct = default);
}

/// <summary>
/// Platform-specific share mounter. One implementation per OS is registered
/// (Windows recycle-bin / freedesktop / Finder follow the same pattern).
/// Picked by <see cref="IPathAccessService"/> by calling
/// <see cref="IsApplicable"/> on each registered instance.
/// </summary>
public interface IPlatformShareMounter
{
    /// <summary>True when this mounter understands the given path (e.g. UNC on Windows).</summary>
    bool IsApplicable(string path);

    /// <summary>
    /// Attaches <paramref name="path"/> using <paramref name="credentials"/>
    /// (when supplied) or the server's ambient identity. Returns the
    /// effective local path the rest of the pipeline can read from.
    /// </summary>
    Task<Result<string>> MountAsync(
        string path, PathCredentials? credentials, Guid cleaningId,
        CancellationToken ct = default);

    /// <summary>Detaches every mount opened for the cleaning.</summary>
    Task<Result> UnmountAsync(Guid cleaningId, CancellationToken ct = default);
}
