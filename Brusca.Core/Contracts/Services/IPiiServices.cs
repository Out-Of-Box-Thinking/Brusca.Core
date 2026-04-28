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
    /// <summary>The content with every detected PII span replaced by its token.</summary>
    public string RedactedContent { get; set; } = string.Empty;

    /// <summary>The list of PII spans that were stripped — original values populated for in-memory use only.</summary>
    public IReadOnlyList<PiiSegment> Segments { get; set; } = [];
}

/// <summary>
/// Re-applies the encrypted PII column back into a previously redacted string.
///
/// The Claude pipeline only ever sees redacted tokens (e.g. <c>[[PII:PersonName:0001]]</c>).
/// When a generated filename, folder template, or user-facing report needs to
/// be materialized for the local file system, the host swaps those tokens
/// back to their original literals using this service.
///
/// PII is sourced from <c>RedactedFileDescriptor.EncryptedPiiJson</c>, decrypted
/// in-process via <see cref="IEncryptionService"/>. Implementations MUST NOT
/// log rehydrated values nor send them off-host.
/// </summary>
public interface IPiiRehydrationService
{
    /// <summary>
    /// Replaces every PII token in <paramref name="redactedText"/> with the
    /// original literal stored against the given redacted file descriptor.
    /// </summary>
    /// <param name="redactedFileId">The descriptor whose encrypted PII column will be consulted.</param>
    /// <param name="redactedText">Text containing zero or more PII tokens.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<string>> RehydrateAsync(
        Guid redactedFileId, string redactedText, CancellationToken ct = default);

    /// <summary>
    /// Resolves PII tokens that may appear inside a <c>DirectoryStructureRule</c>
    /// folder/file path template. Tokens are matched against any redacted
    /// descriptor for the given cleaning that exposes the requested slot.
    /// </summary>
    /// <param name="cleaningId">The cleaning whose redacted descriptors form the lookup pool.</param>
    /// <param name="templatePath">A folder or file template containing zero or more tokens.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<string>> RehydratePathAsync(
        Guid cleaningId, string templatePath, CancellationToken ct = default);
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
/// The input contains ONLY DocumentType + extension counts and the per-type
/// PII slot catalog — never raw PII or file content.
/// </summary>
public interface IClaudeStructureService
{
    Task<DirectoryStructurePlan> AnalyzeStructureAsync(
        Guid cleaningId,
        IReadOnlyList<DocumentTypeSummary> summaries,
        PiiSlotCatalog? slotCatalog = null,
        CancellationToken ct = default);
}

/// <summary>
/// Applies a <see cref="DirectoryStructurePlan"/> against the chosen execution
/// root, using the encrypted-PII column to fill template tokens. Records every
/// before/after move into <see cref="FileRelocationRecord"/>.
/// </summary>
public interface IStructureExecutionService
{
    /// <summary>
    /// Applies the most recently generated <c>DirectoryStructurePlan</c> against
    /// the chosen execution root, recording every move/rename into
    /// <see cref="FileRelocationRecord"/>.
    /// </summary>
    Task<Result<IReadOnlyList<FileRelocationRecord>>> ExecuteStructureAsync(
        Guid cleaningId, CancellationToken ct = default);

    /// <summary>
    /// Computes the same set of relocations <see cref="ExecuteStructureAsync"/>
    /// would produce and persists them with <c>Status = Pending</c> — without
    /// touching the file system. Used by the UI to preview the materialized
    /// layout before committing.
    /// </summary>
    Task<Result<IReadOnlyList<FileRelocationRecord>>> PlanRelocationsAsync(
        Guid cleaningId, CancellationToken ct = default);

    /// <summary>
    /// Reverses every <see cref="FileRelocationRecord"/> for the cleaning whose
    /// <see cref="RelocationStatus"/> is <c>Succeeded</c>, restoring each file
    /// from <c>AfterPath</c> back to <c>BeforePath</c>. Records are flipped to
    /// <c>RolledBack</c>; failures are reported per-record without aborting the
    /// remaining work.
    /// </summary>
    Task<Result<IReadOnlyList<FileRelocationRecord>>> RollbackAsync(
        Guid cleaningId, CancellationToken ct = default);
}

/// <summary>
/// Identifies groups of byte-identical redacted files (same
/// <c>ContentHash</c>) inside a cleaning. Used by
/// <see cref="IStructureExecutionService"/> so duplicates materialize only
/// their elected keeper.
/// </summary>
public interface IDuplicateDetectionService
{
    Task<Result<IReadOnlyList<DuplicateGroup>>> AnalyzeAsync(
        Guid cleaningId, CancellationToken ct = default);
}

/// <summary>
/// Optional, hash-gated, trash-based finalisation step. For every
/// successfully-materialized relocation, verifies the post-move hash matches
/// the original then sends the original to the OS trash via
/// <see cref="ITrashService"/>. Cross-platform (Windows recycle bin,
/// Linux freedesktop trash, macOS Finder trash).
/// </summary>
public interface IPromotionService
{
    Task<Result<IReadOnlyList<PromotionRecord>>> PromoteAsync(
        Guid cleaningId, string userId, CancellationToken ct = default);

    Task<Result<IReadOnlyList<PromotionRecord>>> GetPromotionsAsync(
        Guid cleaningId, CancellationToken ct = default);
}

/// <summary>
/// Produces a sanitized copy of an image whose visible regions contain PII
/// (e.g. signed forms, ID scans, screenshots). The output image has the PII
/// regions occluded — typically a black box or gaussian blur over the
/// bounding boxes returned by OCR — and is what gets moved to the execution
/// target. The original is left untouched on the source path.
///
/// Implementations MUST be local-only: no image bytes may leave the host.
/// </summary>
public interface IImageRedactionService
{
    /// <summary>True when this service can redact the given extension.</summary>
    bool CanRedact(string extension);

    /// <summary>
    /// Reads <paramref name="sourceImagePath"/>, occludes every region in
    /// <paramref name="regions"/>, and writes the sanitized copy to
    /// <paramref name="targetImagePath"/>.
    /// </summary>
    Task<Result<ImageRedactionResult>> RedactAsync(
        string sourceImagePath,
        string targetImagePath,
        IReadOnlyList<ImageRedactionRegion> regions,
        CancellationToken ct = default);
}

/// <summary>
/// A rectangular region inside a source image that should be occluded in
/// the sanitized copy. Coordinates are in source-image pixels.
/// </summary>
public sealed class ImageRedactionRegion
{
    /// <summary>Left edge, in source-image pixels (zero-based).</summary>
    public int X { get; set; }
    /// <summary>Top edge, in source-image pixels (zero-based).</summary>
    public int Y { get; set; }
    /// <summary>Region width, in pixels.</summary>
    public int Width { get; set; }
    /// <summary>Region height, in pixels.</summary>
    public int Height { get; set; }
    /// <summary>Free-form label (e.g. the matching <c>PiiKind</c>) for audit.</summary>
    public string? Label { get; set; }
}

/// <summary>Outcome of a single image-redaction pass.</summary>
public sealed class ImageRedactionResult
{
    /// <summary>Absolute path to the sanitized image written by the service.</summary>
    public string SanitizedImagePath { get; set; } = string.Empty;
    /// <summary>How many regions were occluded.</summary>
    public int RegionsOccluded { get; set; }
    /// <summary>SHA-256 hex digest of the sanitized image bytes for integrity checks.</summary>
    public string? SanitizedContentHash { get; set; }
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

    /// <summary>
    /// Reads <paramref name="filePath"/> and returns the recognized text
    /// together with per-word bounding-box metadata. The host then maps
    /// redacted PII spans (character offsets) back to bounding boxes and
    /// hands the regions to <see cref="IImageRedactionService"/> so the
    /// materialized image copy emerges sanitized.
    ///
    /// Implementations MUST NOT log recognized text.
    /// </summary>
    Task<Result<OcrTextResult>> ExtractTextWithRegionsAsync(
        string filePath, CancellationToken ct = default);
}

/// <summary>
/// Strips identifying metadata (EXIF/XMP/Office core/extended/custom
/// properties, PDF /Info + /Metadata) from a file that has already been
/// materialized to the execution target. Originals are never touched —
/// stripping happens on the redacted copy only.
///
/// Implementations are extension-aware and MUST be local-only.
/// </summary>
public interface IFileMetadataStripper
{
    /// <summary>True when the implementation supports the given extension.</summary>
    bool CanStrip(string extension);

    /// <summary>
    /// Strips identifying metadata from the file at <paramref name="filePath"/>
    /// in-place. Returns <c>Ok</c> with no payload when the file's extension
    /// has no stripper, so callers can apply the pass unconditionally.
    /// </summary>
    Task<Result> StripAsync(
        string filePath, string extension, CancellationToken ct = default);
}

/// <summary>
/// Cross-platform "send to trash" abstraction used by <see cref="IPromotionService"/>.
/// Implementations:
///   ‣ Windows  — recycle bin via Microsoft.VisualBasic.FileIO.FileSystem
///   ‣ Linux    — freedesktop.org Trash spec (~/.local/share/Trash)
///   ‣ macOS    — Finder via osascript, falling back to ~/.Trash
///
/// Originals MUST never be permanently deleted by these implementations —
/// they MUST always remain recoverable from the system trash.
/// </summary>
public interface ITrashService
{
    /// <summary>Sends <paramref name="path"/> to the OS trash/recycle bin.</summary>
    Task<Result> MoveToTrashAsync(string path, CancellationToken ct = default);
}
