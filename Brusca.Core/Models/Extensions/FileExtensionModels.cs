namespace Brusca.Core.Models.Extensions;

/// <summary>
/// Master list of all file extensions ever encountered across all Cleanings.
/// Updated after every scan — new extensions are appended.
/// </summary>
public sealed class FileExtensionRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Normalized lowercase extension including leading dot, e.g. ".pdf"</summary>
    public string Extension { get; set; } = string.Empty;

    public FileExtensionStatus Status { get; set; } = FileExtensionStatus.Unknown;

    /// <summary>Human-friendly description of the file type.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// The NuGet package that provides read access to this file type.
    /// Populated by the UI prompt when an unknown extension is encountered.
    /// </summary>
    public string? ReaderNuGetPackage { get; set; }

    /// <summary>Fully qualified type name of the IFileReader implementation once installed.</summary>
    public string? ReaderImplementationType { get; set; }

    public int TotalTimesEncountered { get; set; }
    public DateTime FirstSeenUtc { get; init; } = DateTime.UtcNow;
    public DateTime LastSeenUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>Result of scanning a path for distinct file extensions.</summary>
public sealed class ExtensionScanResult
{
    public Guid CleaningId { get; set; }
    public IReadOnlyList<string> AllExtensions { get; set; } = [];
    public IReadOnlyList<string> NewExtensions { get; set; } = [];
    public IReadOnlyList<string> UnknownExtensions { get; set; } = [];
    public int TotalFileCount { get; set; }
    public int TotalDirectoryCount { get; set; }
    public DateTime ScannedAtUtc { get; init; } = DateTime.UtcNow;
}
