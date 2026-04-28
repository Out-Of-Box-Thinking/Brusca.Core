namespace Brusca.Core.Enums;

/// <summary>Log level options, configurable in appsettings.json.</summary>
public enum BruscaLogLevel
{
    Trace = 0,
    Debug = 1,
    Information = 2,
    Warning = 3,
    Error = 4,
    Critical = 5,
    None = 6
}

/// <summary>Where logs are written. Set independently for audit and error logs.</summary>
public enum LogSinkTarget
{
    Database = 0,
    File = 1,
    Both = 2,
    Elasticsearch = 3
}

/// <summary>Overall status of a Cleaning run.</summary>
public enum CleaningStatus
{
    Pending = 0,
    Scanning = 1,
    AwaitingExtensionResolution = 2,
    Analyzing = 3,
    PromptGenerated = 4,
    Executing = 5,
    Completed = 6,
    Failed = 7,
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
    Local = 0,
    ActiveDirectory = 1
}

/// <summary>Phase / intent of a Claude-generated prompt step.</summary>
public enum PromptStepType
{
    DirectoryRename = 0,
    FileRename = 1,
    FileMove = 2,
    ContentSummary = 3,
    ConventionSuggestion = 4
}

/// <summary>Status of a file extension — known (built-in reader) or unknown.</summary>
public enum FileExtensionStatus
{
    Known = 0,
    Unknown = 1,
    PendingPackage = 2
}

/// <summary>
/// The scripting language for a PromptStepCommand.
/// Each step can carry commands in one or more languages; the executor
/// picks whichever is available on the host.
/// </summary>
public enum CommandLanguage
{
    CSharp = 0,
    Cmd = 1,
    PowerShell = 2
}

/// <summary>
/// Where the Cleaning will actually make changes.
/// Chosen by the user before execution; always confirmed with a warning.
/// </summary>
public enum ExecutionTarget
{
    /// <summary>Apply changes directly to the original RootPath (double-confirmed).</summary>
    SourcePath = 0,
    /// <summary>Apply changes to an alternate path (safe copy / staging area).</summary>
    AlternatePath = 1
}

/// <summary>
/// Categorization that Claude is allowed to see when planning a directory
/// structure. PII NEVER leaves the host — only this label and the file extension.
/// </summary>
public enum DocumentType
{
    Unknown = 0,
    Invoice = 1,
    Receipt = 2,
    Contract = 3,
    Report = 4,
    Resume = 5,
    MedicalRecord = 6,
    FinancialStatement = 7,
    LegalDocument = 8,
    Correspondence = 9,
    TaxDocument = 10,
    Photo = 11,
    Image = 12,
    Audio = 13,
    Video = 14,
    Spreadsheet = 15,
    Presentation = 16,
    SourceCode = 17,
    Archive = 18,
    Configuration = 19,
    Log = 20,
    PlainText = 21,
    Form = 22,
    Identification = 23
}

/// <summary>
/// Kinds of personally identifiable information a redactor can detect and tokenize.
/// The original literal is stored ONLY in the encrypted PII column.
/// </summary>
public enum PiiKind
{
    PersonName = 0,
    EmailAddress = 1,
    PhoneNumber = 2,
    SocialSecurityNumber = 3,
    CreditCardNumber = 4,
    BankAccountNumber = 5,
    DateOfBirth = 6,
    StreetAddress = 7,
    IpAddress = 8,
    DriversLicense = 9,
    PassportNumber = 10,
    TaxId = 11,
    MedicalRecordNumber = 12,
    VehicleIdentificationNumber = 13,
    Custom = 99
}

/// <summary>What kind of file-system change a relocation record represents.</summary>
public enum RelocationOperationType
{
    Move = 0,
    Rename = 1,
    Copy = 2,
    CreateDirectory = 3,
    Materialize = 4
}

/// <summary>Outcome of an individual relocation entry.</summary>
public enum RelocationStatus
{
    Pending = 0,
    Succeeded = 1,
    Failed = 2,
    Skipped = 3
}
