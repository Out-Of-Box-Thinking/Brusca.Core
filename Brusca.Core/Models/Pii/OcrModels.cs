namespace Brusca.Core.Models.Pii;

/// <summary>
/// One word recognized by an OCR pass, anchored to a pixel-coordinate
/// bounding box in the source image. Used by the PII pipeline to map
/// redacted character spans back onto image regions for occlusion.
///
/// Coordinates are in source-image pixels, zero-based.
/// </summary>
public sealed class OcrWord
{
    public string Text { get; set; } = string.Empty;
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    /// <summary>
    /// Zero-based offset of this word's first character inside the
    /// concatenated <see cref="OcrTextResult.Text"/>. Used to map
    /// redacted PII spans back to bounding boxes.
    /// </summary>
    public int TextOffset { get; set; }
}

/// <summary>
/// Output of an OCR-with-regions pass: the recognized text plus per-word
/// bounding-box metadata. The host stores the box coordinates (NOT the
/// recognized text) so an image redactor can later occlude PII regions.
/// </summary>
public sealed class OcrTextResult
{
    public string Text { get; set; } = string.Empty;
    public IReadOnlyList<OcrWord> Words { get; set; } = [];
}
