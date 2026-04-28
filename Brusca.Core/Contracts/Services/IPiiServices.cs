using Brusca.Core.Enums;
using Brusca.Core.Models.Cleaning;
using Brusca.Core.Models.Pii;
using FluentResults;

namespace Brusca.Core.Contracts.Services;

/// <summary>
/// Detects PII inside file content and returns a redacted copy plus the
/// list of PII spans that were stripped. Implementations MUST NOT log
/// PII values nor leak them across boundaries.
/// </summary>
public interface IPiiRedactionService
{
    /// <summary>
    /// Redact <paramref name="content"/>. Returns the redacted text and the
    /// detected segments (with original values populated for in-memory use only).
    /// </summary>
    Task<Result<PiiRedactionResult>> RedactAsync(
        string content, CancellationToken ct = default);
}

/// <summary>Result of a single redaction pass.</summary>
public sealed class PiiRedactionResult
{
    public string RedactedContent { get; set; } = string.Empty;
    public IReadOnlyList<PiiSegment> Segments { get; set; } = [];
}

/// <summary>
/// Classifies a file as a <see cref="DocumentType"/> using the file extension
/// and the redacted content. Implementations may use heuristics, an embedded
/// model, or a Claude call — but MUST NOT receive un-redacted content.
/// </summary>
public interface IDocumentTypeClassifier
{
    Task<Result<DocumentType>> ClassifyAsync(
        string redactedContent,
        string extension,
        CancellationToken ct = default);
}

/// <summary>
/// Symmetric encryption used to seal the PII JSON column. Implementations
/// SHOULD use ASP.NET Core Data Protection or AES-GCM with a key from a vault.
/// </summary>
public interface IEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}

/// <summary>
/// Asks Claude to design a directory layout from anonymized data only.
/// The input contains ONLY DocumentType + extension counts — never PII or content.
/// </summary>
public interface IClaudeStructureService
{
    Task<DirectoryStructurePlan> AnalyzeStructureAsync(
        Guid cleaningId,
        IReadOnlyList<DocumentTypeSummary> summaries,
        CancellationToken ct = default);
}

/// <summary>
/// Applies a <see cref="DirectoryStructurePlan"/> against the chosen execution
/// root, using the encrypted-PII column to fill template tokens. Records every
/// before/after move into <see cref="FileRelocationRecord"/>.
/// </summary>
public interface IStructureExecutionService
{
    Task<Result<IReadOnlyList<FileRelocationRecord>>> ExecuteStructureAsync(
        Guid cleaningId, CancellationToken ct = default);
}

/// <summary>
/// Extracts text from a binary file format that does not expose plain text
/// directly — primarily images (JPG/JPEG/PNG/GIF/HEIC/HEIF/AVIF/PSD).
///
/// The output of this service feeds <see cref="IPiiRedactionService"/> so that
/// embedded text from screenshots, scans, and photos is redacted BEFORE any
/// content is sent to Claude.
/// </summary>
public interface IOcrService
{
    /// <summary>True when the implementation supports the given extension.</summary>
    bool CanRead(string extension);

    /// <summary>
    /// Reads <paramref name="filePath"/> and returns the recognized text.
    /// Implementations MUST NOT log the recognized text and MUST NOT send
    /// the file or its contents off-host.
    /// </summary>
    Task<Result<string>> ExtractTextAsync(string filePath, CancellationToken ct = default);
}
