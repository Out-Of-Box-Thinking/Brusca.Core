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
    Restarted = 9
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
