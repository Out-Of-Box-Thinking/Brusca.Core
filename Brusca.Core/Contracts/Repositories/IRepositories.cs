using Brusca.Core.Enums;
using Brusca.Core.Models.Cleaning;
using Brusca.Core.Models.Extensions;
using Brusca.Core.Models.Pii;
using FluentResults;

namespace Brusca.Core.Contracts.Repositories;

/// <summary>
/// Cleaning repository. All mutations MUST go through stored procedures.
/// </summary>
public interface ICleaningRepository
{
    Task<Result<Cleaning>> CreateAsync(Cleaning cleaning, CancellationToken ct = default);
    Task<Result<Cleaning>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<Cleaning>>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);
    Task<Result> UpdateStatusAsync(Guid id, CleaningStatus status, CancellationToken ct = default);
    Task<Result> CompleteAsync(Guid id, CancellationToken ct = default);
    Task<Result> AddFileExtensionsAsync(Guid cleaningId, IEnumerable<CleaningFileExtension> extensions, CancellationToken ct = default);
    Task<Result> SetExecutionTargetAsync(Guid id, ExecutionTarget target, string? alternatePath, CancellationToken ct = default);
    Task<Result> SaveTreeSnapshotsAsync(Guid id, string? beforeJson, string? afterJson, CancellationToken ct = default);
    Task<Result> RestartAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Returns the single Cleaning currently held in the working tables
    /// (cleaning.*). Returns a failed result when no active cleaning exists.
    /// Brusca enforces a "one active cleaning at a time" invariant — once a
    /// Cleaning reaches a terminal state and is archived, the working tables
    /// are emptied and a new Cleaning may be started.
    /// </summary>
    Task<Result<Cleaning?>> GetActiveAsync(CancellationToken ct = default);

    /// <summary>
    /// Moves the Cleaning and all of its child rows (file extensions, prompt
    /// steps, redacted descriptors, structure plans, file relocations) from
    /// the working <c>cleaning.*</c> tables into the mirror <c>archive.*</c>
    /// tables, then deletes the originals from the working tables.
    /// Sets <see cref="Cleaning.Status"/> to <c>Archived</c> in the archive copy.
    /// </summary>
    Task<Result> ArchiveAsync(Guid id, CancellationToken ct = default);

    /// <summary>Reads a single archived Cleaning (and children) from <c>archive.*</c>.</summary>
    Task<Result<Cleaning>> GetArchivedByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Pages over the archive (ordered by CreatedAtUtc DESC).</summary>
    Task<Result<IReadOnlyList<Cleaning>>> GetArchivedPagedAsync(int page, int pageSize, CancellationToken ct = default);
}

/// <summary>
/// Prompt step repository. Steps are stored in insertion order per Cleaning.
/// </summary>
public interface IPromptStepRepository
{
    Task<Result<CleaningPromptStep>> CreateAsync(CleaningPromptStep step, CancellationToken ct = default);
    Task<Result<IReadOnlyList<CleaningPromptStep>>> GetByCleaningIdAsync(Guid cleaningId, CancellationToken ct = default);
    Task<Result> UpdateResponseAsync(Guid stepId, string response, CancellationToken ct = default);
    Task<Result> ApproveStepAsync(Guid stepId, CancellationToken ct = default);
    Task<Result> MarkExecutedAsync(Guid stepId, string? error, CancellationToken ct = default);
    Task<Result> DeleteByCleaningIdAsync(Guid cleaningId, CancellationToken ct = default);
}

/// <summary>
/// Prompt step command repository.
/// Commands are child rows of PromptStep — one per language (C#, CMD, PowerShell).
/// </summary>
public interface IPromptStepCommandRepository
{
    Task<Result<PromptStepCommand>> CreateAsync(PromptStepCommand command, CancellationToken ct = default);
    Task<Result> BulkCreateAsync(IEnumerable<PromptStepCommand> commands, CancellationToken ct = default);
    Task<Result<IReadOnlyList<PromptStepCommand>>> GetByStepIdAsync(Guid stepId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<PromptStepCommand>>> GetByCleaningIdAsync(Guid cleaningId, CancellationToken ct = default);
    Task<Result> MarkExecutedAsync(Guid commandId, string? error, CancellationToken ct = default);
    Task<Result> DeleteByCleaningIdAsync(Guid cleaningId, CancellationToken ct = default);
}

/// <summary>
/// Master file extension repository.
/// </summary>
public interface IFileExtensionRepository
{
    Task<Result<IReadOnlyList<FileExtensionRecord>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<FileExtensionRecord?>> GetByExtensionAsync(string extension, CancellationToken ct = default);
    Task<Result> UpsertAsync(FileExtensionRecord record, CancellationToken ct = default);
    Task<Result> BulkUpsertAsync(IEnumerable<FileExtensionRecord> records, CancellationToken ct = default);
    Task<Result> UpdateStatusAsync(string extension, FileExtensionStatus status, string? nuGetPackage, CancellationToken ct = default);
}

/// <summary>
/// Stores per-file redaction descriptors. The PII JSON column is ALREADY
/// encrypted by the calling service before it lands here.
/// </summary>
public interface IRedactedFileRepository
{
    Task<Result<RedactedFileDescriptor>> CreateAsync(RedactedFileDescriptor descriptor, CancellationToken ct = default);
    Task<Result> BulkCreateAsync(IEnumerable<RedactedFileDescriptor> descriptors, CancellationToken ct = default);
    Task<Result<RedactedFileDescriptor>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<RedactedFileDescriptor>>> GetByCleaningIdAsync(Guid cleaningId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<DocumentTypeSummary>>> GetDocumentTypeSummariesAsync(Guid cleaningId, CancellationToken ct = default);
    Task<Result> DeleteByCleaningIdAsync(Guid cleaningId, CancellationToken ct = default);
}

/// <summary>Persists the Claude-generated directory structure plan and its rules.</summary>
public interface IStructurePlanRepository
{
    Task<Result<DirectoryStructurePlan>> CreateAsync(DirectoryStructurePlan plan, CancellationToken ct = default);
    Task<Result<DirectoryStructurePlan>> GetLatestAsync(Guid cleaningId, CancellationToken ct = default);
    Task<Result> DeleteByCleaningIdAsync(Guid cleaningId, CancellationToken ct = default);
}

/// <summary>Records the before/after path of every file or folder operation.</summary>
public interface IFileRelocationRepository
{
    Task<Result<FileRelocationRecord>> CreateAsync(FileRelocationRecord record, CancellationToken ct = default);
    Task<Result> BulkCreateAsync(IEnumerable<FileRelocationRecord> records, CancellationToken ct = default);
    Task<Result> UpdateStatusAsync(Guid id, RelocationStatus status, string? error, CancellationToken ct = default);
    Task<Result<IReadOnlyList<FileRelocationRecord>>> GetByCleaningIdAsync(Guid cleaningId, CancellationToken ct = default);
}
