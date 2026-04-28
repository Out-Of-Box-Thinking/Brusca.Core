using Brusca.Core.Enums;
using Brusca.Core.Models.Cleaning;
using Brusca.Core.Models.Extensions;
using FluentResults;

namespace Brusca.Core.Contracts.Services;

public interface ICleaningService
{
    Task<Result<Cleaning>> StartCleaningAsync(string rootPath, string userId, CancellationToken ct = default);
    Task<Result<ExtensionScanResult>> ScanExtensionsAsync(Guid cleaningId, CancellationToken ct = default);
    Task<Result> GeneratePromptStepsAsync(Guid cleaningId, CancellationToken ct = default);
    Task<Result> SetExecutionTargetAsync(Guid cleaningId, ExecutionTarget target, string? alternatePath, string userId, CancellationToken ct = default);
    Task<Result> ExecuteApprovedStepsAsync(Guid cleaningId, CancellationToken ct = default);
    Task<Result<Cleaning>> GetCleaningAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Resets a halted Cleaning (e.g. AwaitingExtensionResolution) back to Pending
    /// and clears all previous scan data, steps, and commands so it can be
    /// re-processed from scratch after the blocking issue is resolved.
    /// </summary>
    Task<Result> RestartCleaningAsync(Guid cleaningId, string userId, CancellationToken ct = default);

    /// <summary>
    /// Returns the before/after directory tree comparison for the Cleaning.
    /// The "after" tree is projected from approved steps — not yet executed.
    /// </summary>
    Task<Result<TreeComparisonResult>> GetTreeComparisonAsync(Guid cleaningId, CancellationToken ct = default);

    // ── PII redaction + structure-plan flow ──────────────────────────────────

    /// <summary>
    /// Reads every supported file under the cleaning's RootPath, strips PII,
    /// classifies it as a <see cref="Brusca.Core.Enums.DocumentType"/>, and
    /// persists a <c>RedactedFileDescriptor</c> with an encrypted PII JSON column.
    /// PII NEVER leaves the host process unencrypted.
    /// </summary>
    Task<Result<IReadOnlyList<Brusca.Core.Models.Pii.RedactedFileDescriptor>>>
        RedactAndClassifyAsync(Guid cleaningId, CancellationToken ct = default);

    /// <summary>
    /// Asks Claude to design the directory layout using ONLY DocumentType +
    /// extension counts. Saves the result as a <c>DirectoryStructurePlan</c>.
    /// </summary>
    Task<Result<DirectoryStructurePlan>> GenerateStructurePlanAsync(
        Guid cleaningId, CancellationToken ct = default);

    /// <summary>
    /// Returns the latest persisted <c>DirectoryStructurePlan</c> for the cleaning.
    /// </summary>
    Task<Result<DirectoryStructurePlan>> GetStructurePlanAsync(
        Guid cleaningId, CancellationToken ct = default);

    /// <summary>
    /// Computes the relocations that <see cref="ExecuteStructurePlanAsync"/>
    /// would produce and persists them with <c>Status = Pending</c> — without
    /// touching the file system. Lets the UI preview the materialized layout.
    /// </summary>
    Task<Result<IReadOnlyList<FileRelocationRecord>>> PlanStructureRelocationsAsync(
        Guid cleaningId, CancellationToken ct = default);

    /// <summary>
    /// Identifies groups of byte-identical files (same SHA-256 content hash)
    /// inside the cleaning. Used by the UI to surface duplicates to the user
    /// before structure execution skips them.
    /// </summary>
    Task<Result<IReadOnlyList<DuplicateGroup>>> AnalyzeDuplicatesAsync(
        Guid cleaningId, CancellationToken ct = default);

    /// <summary>
    /// Optional, hash-gated, recycle-bin-based finalisation step. For every
    /// successfully materialized relocation, verifies the post-move hash
    /// matches the original then sends the original to the recycle bin.
    /// </summary>
    Task<Result<IReadOnlyList<PromotionRecord>>> PromoteCleaningAsync(
        Guid cleaningId, string userId, CancellationToken ct = default);

    Task<Result<IReadOnlyList<PromotionRecord>>> GetPromotionsAsync(
        Guid cleaningId, CancellationToken ct = default);

    /// <summary>
    /// Applies the most recently generated structure plan against the execution
    /// target — decrypting the PII column to fill template slots — and records
    /// every before/after operation into <c>FileRelocationRecord</c>.
    /// </summary>
    Task<Result<IReadOnlyList<FileRelocationRecord>>> ExecuteStructurePlanAsync(
        Guid cleaningId, CancellationToken ct = default);

    /// <summary>Returns the before/after relocation log for the cleaning.</summary>
    Task<Result<IReadOnlyList<FileRelocationRecord>>> GetRelocationsAsync(
        Guid cleaningId, CancellationToken ct = default);

    /// <summary>
    /// Reverses every successful relocation for the cleaning, restoring each
    /// file from its <c>AfterPath</c> back to its <c>BeforePath</c>. Use this
    /// when an executed structure plan needs to be undone before the cleaning
    /// is archived. Records are updated to <c>RolledBack</c>.
    /// </summary>
    Task<Result<IReadOnlyList<FileRelocationRecord>>> RollbackStructurePlanAsync(
        Guid cleaningId, string userId, CancellationToken ct = default);

    // ── Archive flow ─────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the single Cleaning currently held in the working tables, if any.
    /// Brusca enforces a one-active-cleaning invariant: while a cleaning is
    /// in-flight it lives in <c>cleaning.*</c>; once finished it is moved to
    /// <c>archive.*</c> and the working tables are emptied.
    /// </summary>
    Task<Result<Cleaning?>> GetActiveCleaningAsync(CancellationToken ct = default);

    /// <summary>
    /// Moves a finished Cleaning (and every related row — file extensions,
    /// prompt steps, redacted descriptors, structure plans, file relocations)
    /// from the working tables into the mirror archive tables, then truncates
    /// the working rows. The cleaning's status is set to <c>Archived</c>.
    /// </summary>
    Task<Result> ArchiveCleaningAsync(Guid cleaningId, string userId, CancellationToken ct = default);
}

public interface IFileSystemService
{
    Task<Result<ExtensionScanResult>> ScanForExtensionsAsync(string rootPath, Guid cleaningId, CancellationToken ct = default);
    Task<Result<DirectoryNode>> BuildDirectoryTreeAsync(string rootPath, CancellationToken ct = default);
    Task<Result<string>> ReadFileContentAsync(string filePath, CancellationToken ct = default);
    Task<Result> RenameAsync(string sourcePath, string targetPath, CancellationToken ct = default);
    Task<Result> MoveAsync(string sourcePath, string targetDirectory, CancellationToken ct = default);
    bool IsNetworkShare(string path);
}

public interface IFileReaderService
{
    Task<Result<string>> ReadAsync(string filePath, CancellationToken ct = default);
    bool CanRead(string extension);
    IReadOnlyList<string> SupportedExtensions { get; }
}

public interface IFileExtensionService
{
    Task<Result<IReadOnlyList<FileExtensionRecord>>> GetMasterListAsync(CancellationToken ct = default);
    Task<Result> SyncFromScanAsync(ExtensionScanResult scanResult, CancellationToken ct = default);
    Task<Result<IReadOnlyList<string>>> GetUnknownExtensionsAsync(IEnumerable<string> extensions, CancellationToken ct = default);
    Task<Result> RegisterPackageForExtensionAsync(string extension, string nuGetPackage, CancellationToken ct = default);
}

/// <summary>
/// Projects the "after" directory tree by applying approved step commands
/// to a copy of the before snapshot — without touching the real file system.
/// </summary>
public interface ITreeProjectionService
{
    DirectoryNode ProjectAfterTree(DirectoryNode before, IReadOnlyList<CleaningPromptStep> approvedSteps);
}

/// <summary>
/// Computes file content hashes used by the relocation pipeline to verify
/// integrity across moves between the source path and the execution target
/// (especially across NAS shares). The default implementation is SHA-256.
/// </summary>
public interface IFileHashService
{
    /// <summary>Algorithm name reported in audit rows (e.g. <c>"SHA-256"</c>).</summary>
    string AlgorithmName { get; }

    /// <summary>
    /// Computes the digest of the file at <paramref name="filePath"/> and
    /// returns it as a lowercase hex string.
    /// </summary>
    Task<Result<string>> ComputeAsync(string filePath, CancellationToken ct = default);

    /// <summary>
    /// Compares the digests of two files. Returns <c>true</c> when both files
    /// exist and produce the same digest.
    /// </summary>
    Task<Result<bool>> EqualsAsync(string leftPath, string rightPath, CancellationToken ct = default);
}

/// <summary>
/// Read-only access to runtime secrets. The default implementation in
/// <c>Brusca.Infrastructure</c> is backed by a local Infisical instance when
/// <c>BruscaOptions.Infisical.Enabled == true</c>; otherwise it falls back to
/// <c>IConfiguration</c> so that <c>appsettings.json</c> / environment
/// variables continue to work for development.
///
/// Keys mirror the hierarchical configuration shape, e.g.
/// <c>"DatabaseConnectionString"</c>, <c>"Claude:ApiKey"</c>,
/// <c>"Auth:Jwt:SecretKey"</c>.
/// </summary>
public interface ISecretProvider
{
    /// <summary>Resolves a secret by key. Returns <c>null</c> when not found.</summary>
    Task<string?> GetSecretAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Forces a refresh of any cached secrets from the underlying store.
    /// Called on a timer or on demand when configuration appears stale.
    /// </summary>
    Task RefreshAsync(CancellationToken ct = default);
}
