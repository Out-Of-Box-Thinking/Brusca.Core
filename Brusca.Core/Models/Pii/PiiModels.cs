using Brusca.Core.Enums;

namespace Brusca.Core.Models.Pii;

/// <summary>
/// A single piece of PII located in a file's content.
/// The Token is the placeholder substituted into the redacted text
/// (e.g. "[[PII:PersonName:0001]]"). The Value is the original literal —
/// it MUST only ever be persisted via <see cref="RedactedFileDescriptor.EncryptedPiiJson"/>.
/// </summary>
public sealed class PiiSegment
{
    /// <summary>Stable id within a file's segment list (also the integer in the Token).</summary>
    public int Ordinal { get; set; }

    /// <summary>What kind of PII this is.</summary>
    public PiiKind Kind { get; set; }

    /// <summary>The original literal — handle ONLY in memory or encrypted at rest.</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>The placeholder token written into the redacted text.</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>Zero-based start index in the ORIGINAL content.</summary>
    public int StartIndex { get; set; }

    /// <summary>Length of the matched span.</summary>
    public int Length { get; set; }

    /// <summary>Free-form label (e.g. regex name, custom rule name).</summary>
    public string? Label { get; set; }
}

/// <summary>
/// One row per file that has been read by a supported reader, redacted of PII,
/// and classified by document type.
///
/// Stored in <c>cleaning.RedactedFile</c>:
///   ‣ <see cref="EncryptedPiiJson"/> is an encrypted JSON blob of the original PII —
///     decrypted only at execution time when target paths/names are materialized.
///   ‣ <see cref="DocumentType"/> + <see cref="Extension"/> are the ONLY labels
///     sent to Claude when generating the directory structure plan.
/// </summary>
public sealed class RedactedFileDescriptor
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid CleaningId { get; set; }

    /// <summary>The original absolute path on disk at scan time.</summary>
    public string OriginalFilePath { get; set; } = string.Empty;

    /// <summary>The original file name (with extension).</summary>
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>Normalized lowercase extension including leading dot, e.g. ".pdf".</summary>
    public string Extension { get; set; } = string.Empty;

    /// <summary>Claude-visible category for this file.</summary>
    public DocumentType DocumentType { get; set; } = DocumentType.Unknown;

    /// <summary>
    /// File content with every PII span replaced by a stable token
    /// (e.g. "[[PII:PersonName:0001]]"). Safe to send to Claude.
    /// </summary>
    public string RedactedContent { get; set; } = string.Empty;

    /// <summary>
    /// Encrypted JSON document containing the original <see cref="PiiSegment"/> list.
    /// Encryption is performed by <c>IEncryptionService</c>.
    /// </summary>
    public string? EncryptedPiiJson { get; set; }

    /// <summary>How many PII spans were detected (for UI / audit; never the values).</summary>
    public int PiiSegmentCount { get; set; }

    /// <summary>Hash of the original file content (SHA-256, hex) for integrity checks.</summary>
    public string? ContentHash { get; set; }

    /// <summary>
    /// JSON-serialized array of <c>ImageRedactionRegion</c> coordinates
    /// (no PII text — bounding boxes only) computed during redaction for
    /// image files. Consumed by <c>IImageRedactionService</c> at
    /// materialization time so the image copy emerges with PII regions
    /// occluded. Null for non-image content or when no PII was detected.
    /// </summary>
    public string? ImageRedactionRegionsJson { get; set; }

    /// <summary>
    /// Plaintext JSON map (slot name → <see cref="PiiSegment.Ordinal"/>) produced
    /// by <c>IPiiSlotMappingService</c> after Claude has published the structure
    /// plan's <c>RequiredTokenSlots</c>. Slot names and ordinals are NOT PII;
    /// resolving a slot to its literal still requires decrypting
    /// <see cref="EncryptedPiiJson"/>.
    /// </summary>
    public string? SlotMapJson { get; set; }

    public DateTime DiscoveredAtUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Aggregate of redacted descriptors used as the ONLY input Claude receives
/// when planning the directory layout.
/// </summary>
public sealed class DocumentTypeSummary
{
    public DocumentType DocumentType { get; set; }
    public string Extension { get; set; } = string.Empty;
    public int Count { get; set; }
}

/// <summary>
/// The vocabulary of token slots available, per <see cref="DocumentType"/>,
/// across the redacted corpus of one cleaning. Sent to Claude alongside
/// <see cref="DocumentTypeSummary"/> rows so Claude only emits
/// <c>RequiredTokenSlots</c> values that the host can actually substitute.
/// </summary>
public sealed class PiiSlotCatalog
{
    public Guid CleaningId { get; init; }
    public IReadOnlyList<DocumentTypeSlotEntry> Entries { get; init; } = [];
}

/// <summary>One row of the <see cref="PiiSlotCatalog"/>.</summary>
public sealed class DocumentTypeSlotEntry
{
    public DocumentType DocumentType { get; set; }
    /// <summary>The PII kinds present at least once across files of this DocumentType.</summary>
    public IReadOnlyList<PiiKind> AvailableKinds { get; set; } = [];
}
