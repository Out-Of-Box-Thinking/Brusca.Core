using Brusca.Core.Enums;

namespace Brusca.Core.Models.Extensions;

/// <summary>
/// Catalog of file extensions Brusca is expected to recognize and read.
/// Used to seed <see cref="FileExtensionRecord"/> rows on first run and to
/// drive content-extraction strategy (text vs binary vs OCR-required image).
///
/// Image formats listed under <see cref="OcrImageExtensions"/> still flow
/// through the PII redaction pipeline: an OCR pass extracts text first,
/// which is then handed to <c>IPiiRedactionService</c> BEFORE any data is
/// shared with Claude.
/// </summary>
public static class KnownFileExtensions
{
    /// <summary>How content should be pulled out of a file before redaction.</summary>
    public enum ExtractionMode
    {
        /// <summary>Read directly as plain text (UTF-8 / detected codepage).</summary>
        Text,
        /// <summary>Use a structured-document reader (Open XML, ODF, PDF, etc.).</summary>
        StructuredDocument,
        /// <summary>Use a spreadsheet reader and flatten cells to text.</summary>
        Spreadsheet,
        /// <summary>Run OCR (e.g. Tesseract) to extract embedded text from an image.</summary>
        Ocr
    }

    /// <summary>Descriptor for a single well-known extension.</summary>
    public sealed record KnownExtension(
        string Extension,
        string Description,
        DocumentType DefaultDocumentType,
        ExtractionMode Mode,
        string? SuggestedReaderNuGetPackage);

    /// <summary>
    /// Authoritative seed list. Order is preserved for deterministic seeding.
    /// All extensions are normalized to lowercase including the leading dot.
    /// </summary>
    public static readonly IReadOnlyList<KnownExtension> All =
    [
        // ── Text & plain documents ──────────────────────────────────────────
        new(".txt",     "Plain text file",                                  DocumentType.PlainText,        ExtractionMode.Text,                null),
        new(".rtf",     "Rich Text Format",                                 DocumentType.PlainText,        ExtractionMode.StructuredDocument,  "RtfPipe"),

        // ── Word-processor documents ────────────────────────────────────────
        new(".docx",    "Microsoft Word Open XML document",                 DocumentType.Report,           ExtractionMode.StructuredDocument,  "DocumentFormat.OpenXml"),
        new(".odt",     "OpenDocument Text",                                DocumentType.Report,           ExtractionMode.StructuredDocument,  "AODL"),
        new(".pages",   "Apple Pages document",                             DocumentType.Report,           ExtractionMode.StructuredDocument,  "Brusca.Readers.Pages"),
        new(".pdf",     "Portable Document Format",                         DocumentType.Report,           ExtractionMode.StructuredDocument,  "UglyToad.PdfPig"),

        // ── Spreadsheets ────────────────────────────────────────────────────
        new(".csv",     "Comma-Separated Values",                           DocumentType.Spreadsheet,      ExtractionMode.Text,                null),
        new(".xlsx",    "Microsoft Excel Open XML spreadsheet",             DocumentType.Spreadsheet,      ExtractionMode.Spreadsheet,         "ClosedXML"),
        new(".ods",     "OpenDocument Spreadsheet",                         DocumentType.Spreadsheet,      ExtractionMode.Spreadsheet,         "AODL"),
        new(".numbers", "Apple Numbers spreadsheet",                        DocumentType.Spreadsheet,      ExtractionMode.Spreadsheet,         "Brusca.Readers.Numbers"),

        // ── Images (OCR-required so embedded text is redacted) ──────────────
        new(".jpg",     "JPEG compressed image",                            DocumentType.Image,            ExtractionMode.Ocr,                 "Tesseract"),
        new(".jpeg",    "JPEG compressed image",                            DocumentType.Image,            ExtractionMode.Ocr,                 "Tesseract"),
        new(".png",     "Portable Network Graphics",                        DocumentType.Image,            ExtractionMode.Ocr,                 "Tesseract"),
        new(".gif",     "Graphics Interchange Format",                      DocumentType.Image,            ExtractionMode.Ocr,                 "Tesseract"),
        new(".heic",    "High Efficiency Image Container",                  DocumentType.Image,            ExtractionMode.Ocr,                 "Magick.NET-Q8-AnyCPU"),
        new(".heif",    "High Efficiency Image Format",                     DocumentType.Image,            ExtractionMode.Ocr,                 "Magick.NET-Q8-AnyCPU"),
        new(".avif",    "AV1 Image File Format",                            DocumentType.Image,            ExtractionMode.Ocr,                 "Magick.NET-Q8-AnyCPU"),
        new(".psd",     "Adobe Photoshop Document",                         DocumentType.Image,            ExtractionMode.Ocr,                 "Magick.NET-Q8-AnyCPU"),
    ];

    /// <summary>Set of extensions that require OCR before redaction.</summary>
    public static readonly IReadOnlyCollection<string> OcrImageExtensions =
        All.Where(x => x.Mode == ExtractionMode.Ocr)
           .Select(x => x.Extension)
           .ToArray();

    /// <summary>Lookup by normalized lowercase extension (with leading dot).</summary>
    public static readonly IReadOnlyDictionary<string, KnownExtension> ByExtension =
        All.ToDictionary(x => x.Extension, StringComparer.OrdinalIgnoreCase);

    /// <summary>True when Brusca knows a reader strategy for the extension.</summary>
    public static bool IsKnown(string extension) =>
        !string.IsNullOrWhiteSpace(extension) && ByExtension.ContainsKey(extension.ToLowerInvariant());

    /// <summary>True when the extension represents an image that requires OCR.</summary>
    public static bool RequiresOcr(string extension) =>
        !string.IsNullOrWhiteSpace(extension)
        && ByExtension.TryGetValue(extension.ToLowerInvariant(), out var k)
        && k.Mode == ExtractionMode.Ocr;
}
