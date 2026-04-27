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
