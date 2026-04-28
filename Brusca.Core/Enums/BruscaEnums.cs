namespace Brusca.Core.Enums;

/// <summary>Log level options, configurable in appsettings.json.</summary>
public enum BruscaLogLevel
{
    /// <summary>Most verbose — internal diagnostics.</summary>
    Trace = 0,
    /// <summary>Debugging information for developers.</summary>
    Debug = 1,
    /// <summary>Normal operational messages.</summary>
    Information = 2,
    /// <summary>A condition that may require attention but does not stop execution.</summary>
    Warning = 3,
    /// <summary>A handled error that prevented the current operation.</summary>
    Error = 4,
    /// <summary>A failure that requires immediate attention; the process may not continue.</summary>
    Critical = 5,
    /// <summary>Logging is disabled.</summary>
    None = 6
}

/// <summary>Where logs are written. Set independently for audit and error logs.</summary>
public enum LogSinkTarget
{
    /// <summary>Write log entries to the SQL Server database sink.</summary>
    Database = 0,
    /// <summary>Write log entries to a rolling file on disk.</summary>
    File = 1,
    /// <summary>Write to both the database and file sinks.</summary>
    Both = 2,
    /// <summary>Ship log entries to an Elasticsearch cluster.</summary>
    Elasticsearch = 3
}

/// <summary>Overall status of a Cleaning run.</summary>
public enum CleaningStatus
{
    /// <summary>Created but no work has started yet.</summary>
    Pending = 0,
    /// <summary>Walking the file system and gathering extensions.</summary>
    Scanning = 1,
    /// <summary>Halted because one or more extensions have no installed reader.</summary>
    AwaitingExtensionResolution = 2,
    /// <summary>Asking Claude (legacy flow) to analyze the directory tree.</summary>
    Analyzing = 3,
    /// <summary>Claude has produced prompt steps awaiting user approval.</summary>
    PromptGenerated = 4,
    /// <summary>Approved prompt steps are being executed against the file system.</summary>
    Executing = 5,
    /// <summary>All work finished successfully.</summary>
    Completed = 6,
    /// <summary>Run terminated with an unrecoverable error.</summary>
    Failed = 7,
    /// <summary>The user cancelled the run before it completed.</summary>
    Cancelled = 8,
    /// <summary>
    /// The Cleaning was halted (e.g. unknown extension) and has been
    /// reset by the user for a fresh scan from the beginning.
    /// </summary>
    Restarted = 9,
    /// <summary>Running PII redaction + document-type classification.</summary>
    Redacting = 10,
    /// <summary>Redaction complete; descriptors persisted with encrypted PII.</summary>
    Redacted = 11,
    /// <summary>Claude has produced a structure plan based on anonymized descriptors only.</summary>
    StructurePlanGenerated = 12,
    /// <summary>Applying the structure plan against the chosen execution root.</summary>
    StructureExecuting = 13,
    /// <summary>
    /// Cleaning has finished and all of its rows have been moved out of the
    /// working tables (cleaning.*) into the matching archive tables
    /// (archive.*). Only one Cleaning may be un-archived at a time.
    /// </summary>
    Archived = 14
}

/// <summary>Authentication mode, controlled in appsettings.json.</summary>
public enum AuthenticationMode
{
    /// <summary>Username + password validated against the local users table.</summary>
    Local = 0,
    /// <summary>Authenticate against on-premises Active Directory / Entra ID.</summary>
    ActiveDirectory = 1
}

/// <summary>Phase / intent of a Claude-generated prompt step.</summary>
public enum PromptStepType
{
    /// <summary>Rename a directory.</summary>
    DirectoryRename = 0,
    /// <summary>Rename a single file.</summary>
    FileRename = 1,
    /// <summary>Move a file from one directory to another.</summary>
    FileMove = 2,
    /// <summary>Produce a textual summary of file content.</summary>
    ContentSummary = 3,
    /// <summary>Suggest a naming or layout convention for the user.</summary>
    ConventionSuggestion = 4
}

/// <summary>Status of a file extension — known (built-in reader) or unknown.</summary>
public enum FileExtensionStatus
{
    /// <summary>A reader for this extension is available and registered.</summary>
    Known = 0,
    /// <summary>The extension has been seen but no reader is registered.</summary>
    Unknown = 1,
    /// <summary>The user has nominated a NuGet package; awaiting installation.</summary>
    PendingPackage = 2
}

/// <summary>
/// The scripting language for a PromptStepCommand.
/// Each step can carry commands in one or more languages; the executor
/// picks whichever is available on the host.
/// </summary>
public enum CommandLanguage
{
    /// <summary>A C# snippet using <c>System.IO</c> APIs.</summary>
    CSharp = 0,
    /// <summary>A classic Windows CMD batch command.</summary>
    Cmd = 1,
    /// <summary>A PowerShell command or script block.</summary>
    PowerShell = 2
}

/// <summary>
/// Where the Cleaning will materialize its organized output.
///
/// Brusca treats the original source files as <b>strictly read-only</b> at all
/// times. Structure execution therefore always <i>copies</i> files into the
/// target layout — it never moves, renames, or deletes anything under
/// <c>RootPath</c>. The choice below only governs whether the materialized
/// copies land inside the original root (under new subfolders) or in a
/// completely separate staging directory.
/// </summary>
public enum ExecutionTarget
{
    /// <summary>
    /// Materialize copies under the original <c>RootPath</c> (in newly created
    /// subfolders). Originals remain untouched — this still produces copies, never moves.
    /// </summary>
    SourcePath = 0,
    /// <summary>
    /// Materialize copies under <c>AlternateExecutionPath</c> — the recommended
    /// option. Originals remain completely untouched at <c>RootPath</c>.
    /// </summary>
    AlternatePath = 1
}

/// <summary>
/// Categorization that Claude is allowed to see when planning a directory
/// structure. PII NEVER leaves the host — only this label and the file extension.
/// </summary>
public enum DocumentType
{
    /// <summary>Could not be classified.</summary>
    Unknown = 0,
    /// <summary>An invoice (billing document).</summary>
    Invoice = 1,
    /// <summary>A receipt (proof of purchase).</summary>
    Receipt = 2,
    /// <summary>A contractual agreement.</summary>
    Contract = 3,
    /// <summary>A general business report.</summary>
    Report = 4,
    /// <summary>A résumé / CV.</summary>
    Resume = 5,
    /// <summary>A medical record or clinical note.</summary>
    MedicalRecord = 6,
    /// <summary>A bank or brokerage financial statement.</summary>
    FinancialStatement = 7,
    /// <summary>A general legal document.</summary>
    LegalDocument = 8,
    /// <summary>Letters, emails, memos, and other correspondence.</summary>
    Correspondence = 9,
    /// <summary>Tax forms, returns, and supporting tax documents.</summary>
    TaxDocument = 10,
    /// <summary>A personal photograph.</summary>
    Photo = 11,
    /// <summary>A non-photographic image (diagram, screenshot, scan).</summary>
    Image = 12,
    /// <summary>An audio file.</summary>
    Audio = 13,
    /// <summary>A video file.</summary>
    Video = 14,
    /// <summary>A spreadsheet (.xlsx, .ods, .numbers, .csv).</summary>
    Spreadsheet = 15,
    /// <summary>A slide-deck presentation.</summary>
    Presentation = 16,
    /// <summary>Source code in any programming language.</summary>
    SourceCode = 17,
    /// <summary>A compressed archive (zip/tar/7z/etc.).</summary>
    Archive = 18,
    /// <summary>An application configuration file.</summary>
    Configuration = 19,
    /// <summary>A log file.</summary>
    Log = 20,
    /// <summary>Plain unstructured text.</summary>
    PlainText = 21,
    /// <summary>A fillable form.</summary>
    Form = 22,
    /// <summary>An identification document (passport, license, etc.).</summary>
    Identification = 23
}

/// <summary>
/// Kinds of personally identifiable information a redactor can detect and tokenize.
/// The original literal is stored ONLY in the encrypted PII column.
/// </summary>
public enum PiiKind
{
    /// <summary>The name of a person.</summary>
    PersonName = 0,
    /// <summary>An email address.</summary>
    EmailAddress = 1,
    /// <summary>A phone number in any common format.</summary>
    PhoneNumber = 2,
    /// <summary>A US Social Security Number.</summary>
    SocialSecurityNumber = 3,
    /// <summary>A credit card number.</summary>
    CreditCardNumber = 4,
    /// <summary>A bank account number.</summary>
    BankAccountNumber = 5,
    /// <summary>A date of birth.</summary>
    DateOfBirth = 6,
    /// <summary>A street / postal address.</summary>
    StreetAddress = 7,
    /// <summary>An IPv4 or IPv6 address.</summary>
    IpAddress = 8,
    /// <summary>A driver license number.</summary>
    DriversLicense = 9,
    /// <summary>A passport number.</summary>
    PassportNumber = 10,
    /// <summary>A tax identification number (e.g. EIN, ITIN).</summary>
    TaxId = 11,
    /// <summary>A medical record number assigned by a healthcare provider.</summary>
    MedicalRecordNumber = 12,
    /// <summary>A vehicle identification number (VIN).</summary>
    VehicleIdentificationNumber = 13,
    /// <summary>A custom rule contributed by configuration.</summary>
    Custom = 99
}

/// <summary>
/// What kind of file-system change a relocation record represents.
///
/// Note: Brusca's structure-execution pipeline keeps original files
/// strictly read-only, so concrete file operations always produce
/// <see cref="Materialize"/> (or <see cref="CreateDirectory"/>) records.
/// The <see cref="Move"/> / <see cref="Rename"/> values are retained only
/// for legacy prompt-step records.
/// </summary>
public enum RelocationOperationType
{
    /// <summary>(Legacy) Move a file or folder to a new location.</summary>
    Move = 0,
    /// <summary>(Legacy) Rename a file or folder in place.</summary>
    Rename = 1,
    /// <summary>Copy a file or folder, leaving the original untouched.</summary>
    Copy = 2,
    /// <summary>Create a new directory.</summary>
    CreateDirectory = 3,
    /// <summary>Materialize a templated path/file from a structure plan (always a copy).</summary>
    Materialize = 4,
    /// <summary>
    /// The source file was identified as a duplicate of another file in the
    /// same cleaning (same <c>ContentHash</c>) and was deliberately skipped
    /// by the deduplication pass. No copy is performed; the chosen
    /// representative is materialized normally.
    /// </summary>
    SkipDuplicate = 5
}

/// <summary>
/// Behaviour when a structure-plan materialize would write to a path that
/// already exists at the destination.
/// </summary>
public enum MaterializationCollisionPolicy
{
    /// <summary>Throw an <see cref="System.IO.IOException"/> and record the file as <c>Failed</c>.</summary>
    Fail = 0,
    /// <summary>Append an ascending <c>_(2)</c>, <c>_(3)</c> suffix until a free name is found.</summary>
    Suffix = 1,
    /// <summary>Skip the materialize and record the file as <c>Skipped</c>.</summary>
    Skip = 2
}

/// <summary>
/// Lifecycle state of a <c>PromotionRecord</c>. Promotion is the optional,
/// hash-gated, recycle-bin-based step that replaces the original files with
/// the materialized copy once the user has verified the plan.
/// </summary>
public enum PromotionStatus
{
    /// <summary>Created but the post-materialize hash check has not run yet.</summary>
    Pending = 0,
    /// <summary>The materialized copy's hash matches the original — safe to promote.</summary>
    Verified = 1,
    /// <summary>The original was deleted to the recycle bin and the copy is now canonical.</summary>
    Promoted = 2,
    /// <summary>Promotion failed — see <c>PromotionRecord.ErrorMessage</c>.</summary>
    Failed = 3
}

/// <summary>
/// Strategy used by the duplicate-detection pass to choose the single
/// representative file inside a duplicate group.
/// </summary>
public enum DuplicateKeepStrategy
{
    /// <summary>Keep the file with the most recent <c>DiscoveredAtUtc</c>.</summary>
    KeepNewest = 0,
    /// <summary>Keep the file whose <c>OriginalFilePath</c> is alphabetically first.</summary>
    KeepFirstPath = 1,
    /// <summary>Keep the file whose <c>OriginalFilePath</c> is the longest (deepest folder).</summary>
    KeepDeepestPath = 2
}

/// <summary>Outcome of an individual relocation entry.</summary>
public enum RelocationStatus
{
    /// <summary>Created but not yet executed.</summary>
    Pending = 0,
    /// <summary>Executed without error.</summary>
    Succeeded = 1,
    /// <summary>Execution raised an error; see ErrorMessage.</summary>
    Failed = 2,
    /// <summary>Execution was deliberately skipped (e.g. duplicate target).</summary>
    Skipped = 3,
    /// <summary>The previously-succeeded relocation was reversed by a rollback.</summary>
    RolledBack = 4
}
