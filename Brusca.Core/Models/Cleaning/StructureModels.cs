using Brusca.Core.Enums;

namespace Brusca.Core.Models.Cleaning;

/// <summary>
/// A directory layout plan produced by Claude using ONLY anonymized
/// <c>DocumentType</c> + extension counts. Contains no PII.
/// At execution time the host substitutes encrypted-PII tokens into the
/// templates to materialize concrete folder/file paths.
/// </summary>
public sealed class DirectoryStructurePlan
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid CleaningId { get; set; }

    /// <summary>Human-readable summary of the convention Claude proposed.</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>Ordered rules — one per matched DocumentType bucket.</summary>
    public IReadOnlyList<DirectoryStructureRule> Rules { get; set; } = [];

    public DateTime GeneratedAtUtc { get; init; } = DateTime.UtcNow;

    /// <summary>Raw JSON Claude returned, kept for audit and replay.</summary>
    public string? RawPlanJson { get; set; }
}

/// <summary>
/// A single rule for materializing files of one DocumentType.
///
/// Templates may reference PII tokens which the host resolves at execution time
/// from the encrypted PII column, e.g.:
///   FolderPathTemplate = "Invoices/{{Year}}/{{ClientName}}"
///   FileNameTemplate   = "{{InvoiceNumber}}_{{Date}}{Extension}"
/// </summary>
public sealed class DirectoryStructureRule
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DocumentType DocumentType { get; set; }
    public string Extension { get; set; } = string.Empty;

    /// <summary>Template for the destination folder path (relative to the execution root).</summary>
    public string FolderPathTemplate { get; set; } = string.Empty;

    /// <summary>Template for the new file name (extension is appended automatically).</summary>
    public string FileNameTemplate { get; set; } = string.Empty;

    /// <summary>Token slot names referenced by the templates (e.g. ClientName, Year).</summary>
    public IReadOnlyList<string> RequiredTokenSlots { get; set; } = [];

    /// <summary>Free-form notes Claude attached to this rule.</summary>
    public string? Rationale { get; set; }

    public int Order { get; set; }

    /// <summary>When this rule row was persisted.</summary>
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// One row per concrete file/folder operation performed during structure execution.
/// Captures the BEFORE and AFTER state for full audit traceability whether the
/// operation was performed against the source path or an alternate path.
/// </summary>
public sealed class FileRelocationRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid CleaningId { get; set; }

    /// <summary>The redacted file descriptor this record materializes (null for pure folder ops).</summary>
    public Guid? RedactedFileId { get; set; }

    public RelocationOperationType OperationType { get; set; }
    public ExecutionTarget ExecutionTarget { get; set; }

    /// <summary>BEFORE — original absolute path.</summary>
    public string? BeforePath { get; set; }
    /// <summary>BEFORE — original name (file or folder).</summary>
    public string? BeforeName { get; set; }

    /// <summary>AFTER — final absolute path.</summary>
    public string? AfterPath { get; set; }
    /// <summary>AFTER — final name (file or folder).</summary>
    public string? AfterName { get; set; }

    public RelocationStatus Status { get; set; } = RelocationStatus.Pending;
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
}
