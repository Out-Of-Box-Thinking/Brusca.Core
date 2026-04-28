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

    /// <summary>
    /// SHA-256 hex digest of the file at <see cref="AfterPath"/> immediately
    /// after the relocation completed. Combined with
    /// <see cref="Brusca.Core.Models.Pii.RedactedFileDescriptor.ContentHash"/>
    /// (the before-digest) this proves the move did not corrupt the file.
    /// </summary>
    public string? ContentHashAfter { get; set; }
}

/// <summary>
/// A set of <see cref="Brusca.Core.Models.Pii.RedactedFileDescriptor"/> rows
/// that share the same <c>ContentHash</c> — i.e. byte-identical duplicates.
/// One file in the group is elected the keeper and materialized normally;
/// the rest are recorded as <see cref="RelocationOperationType.SkipDuplicate"/>
/// rows so the audit log shows them but no extra copy is produced.
/// </summary>
public sealed class DuplicateGroup
{
    public string ContentHash { get; set; } = string.Empty;
    /// <summary>The chosen representative file (kept).</summary>
    public Guid KeepRedactedFileId { get; set; }
    /// <summary>Every redacted-file id in the group (including the keeper).</summary>
    public IReadOnlyList<Guid> RedactedFileIds { get; set; } = [];
    /// <summary>Strategy used to pick the keeper.</summary>
    public DuplicateKeepStrategy Strategy { get; set; } = DuplicateKeepStrategy.KeepFirstPath;
}

/// <summary>
/// One row per file the user has elected to <i>promote</i> — i.e. delete
/// the original to the recycle bin now that the materialized copy has been
/// verified by hash. Promotion is a deliberate, opt-in step that never runs
/// automatically: structure execution always produces copies first; promotion
/// is the explicit second pass that finishes the cleanup.
/// </summary>
public sealed class PromotionRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid CleaningId { get; set; }

    /// <summary>The relocation that produced the materialized copy this record promotes.</summary>
    public Guid FileRelocationId { get; set; }

    /// <summary>The absolute path of the original that was sent to the recycle bin.</summary>
    public string OriginalPath { get; set; } = string.Empty;

    public PromotionStatus Status { get; set; } = PromotionStatus.Pending;
    public string? ErrorMessage { get; set; }

    public DateTime? VerifiedAtUtc { get; set; }
    public DateTime? PromotedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
}
