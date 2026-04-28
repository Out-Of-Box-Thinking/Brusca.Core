namespace Brusca.Core.Models.Pii;

/// <summary>
/// Per-file mapping from a directory-structure-rule slot name (e.g.
/// <c>"ClientName"</c>, <c>"InvoiceNumber"</c>) to the <see cref="PiiSegment.Ordinal"/>
/// in the file's encrypted PII segment list that supplies the literal value.
///
/// The map is stored PLAINTEXT in <c>cleaning.RedactedFile.SlotMapJson</c>:
/// slot names are vocabulary published by Claude in the structure plan and
/// ordinals are integers — neither carries PII. Decryption of
/// <see cref="RedactedFileDescriptor.EncryptedPiiJson"/> is still required
/// at materialization time to actually USE the map.
/// </summary>
public sealed class PiiSlotMap
{
    public Guid RedactedFileId { get; set; }

    /// <summary>Slot name → ordinal of the matching <see cref="PiiSegment"/>.</summary>
    public Dictionary<string, int> SlotToOrdinal { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Pre-execute report row identifying a file that the rehydrator will not be
/// able to substitute into its target template because one or more
/// <c>RequiredTokenSlots</c> are unmapped or point at an empty PII segment.
/// </summary>
public sealed class MissingSlotReport
{
    public Guid RedactedFileId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public IReadOnlyList<string> MissingSlots { get; set; } = [];

    /// <summary>
    /// The path the file would land at if execution proceeded with the current
    /// (incomplete) slot map — typically a fallback location chosen by the
    /// validator (e.g. an "_unsorted" bucket). Null if no fallback exists.
    /// </summary>
    public string? FallbackPath { get; set; }
}
