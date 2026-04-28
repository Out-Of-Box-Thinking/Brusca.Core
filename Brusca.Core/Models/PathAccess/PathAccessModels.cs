namespace Brusca.Core.Models.PathAccess;

/// <summary>
/// Outcome of probing a path the user typed into the UI as a Cleaning's
/// source root or execution target. The host uses this to decide whether
/// to (a) proceed, (b) prompt the browser for share credentials, or
/// (c) reject the path because it appears to live on the user's local
/// machine and is therefore unreachable from the server.
/// </summary>
public sealed class PathProbeResult
{
    /// <summary>The path that was probed (echoed back for the UI).</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>True when the server can list the directory right now.</summary>
    public bool IsReachable { get; set; }

    /// <summary>
    /// True when the path looks like a remote share (UNC/SMB/NFS) but the
    /// server cannot list it — typically because no credential is mounted
    /// for the user account. The UI should prompt for username/password.
    /// </summary>
    public bool RequiresCredentials { get; set; }

    /// <summary>
    /// True when the path looks like it points at the user's local machine
    /// (e.g. <c>C:\Users\...</c>, <c>/Users/...</c>, <c>/home/...</c>) but
    /// the server cannot see it. Browsers cannot expose a local filesystem
    /// to a remote server; the UI should refuse with a clear diagnostic.
    /// </summary>
    public bool IsLikelyClientLocal { get; set; }

    /// <summary>Free-form diagnostic message for the UI.</summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Credentials supplied by the user (over TLS, once) so the server can
/// open a connection to a remote share. The cleartext password lives in
/// memory only inside <see cref="IPathAccessService.MountAsync"/>; the
/// persisted form is encrypted via <c>IEncryptionService</c>.
/// </summary>
public sealed class PathCredentials
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Domain { get; set; }
}

/// <summary>
/// Token returned by <see cref="IPathAccessService.MountAsync"/> describing
/// a successful mount. Always returned even for class-1 (server-local)
/// paths — in that case <see cref="IsNoOp"/> is true and no platform call
/// was made.
/// </summary>
public sealed class PathMountHandle
{
    public Guid CleaningId { get; set; }
    public string RemotePath { get; set; } = string.Empty;
    public string? LocalMountPoint { get; set; }
    public bool IsNoOp { get; set; }
    public DateTime MountedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Persisted form of <see cref="PathCredentials"/>. Stored in
/// <c>cleaning.PathCredential</c> with <see cref="EncryptedPassword"/> sealed
/// by the same <c>IEncryptionService</c> that protects the PII column.
/// Rows are deleted when the cleaning is archived.
/// </summary>
public sealed class PathCredentialRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid CleaningId { get; set; }
    public string RootPath { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string EncryptedPassword { get; set; } = string.Empty;
    public string? Domain { get; set; }
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
    public DateTime? LastUsedAtUtc { get; set; }
}
