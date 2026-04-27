using Brusca.Core.Enums;
using Brusca.Core.Models.Cleaning;
using Brusca.Core.Models.Extensions;
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
