using Brusca.Core.Enums;

namespace Brusca.Core.Models;

/// <summary>
/// Root options bound from appsettings.json section "Brusca".
/// All application configuration — including the database connection string —
/// lives under this single key for unified management.
/// </summary>
public sealed class BruscaOptions
{
    /// <summary>
    /// The single, authoritative database connection string for all of Brusca.
    /// Used by DapperRepositoryBase, Serilog MSSqlServer sink, and Audit.NET provider.
    /// Set in appsettings.json for development; use environment variable
    /// Brusca__DatabaseConnectionString for production deployments.
    /// </summary>
    public string DatabaseConnectionString { get; set; } = string.Empty;

    public AuthOptions Auth { get; set; } = new();
    public CorsOptions Cors { get; set; } = new();
    public LoggingOptions Logging { get; set; } = new();
    public ClaudeOptions Claude { get; set; } = new();
    public FileSystemOptions FileSystem { get; set; } = new();
    public PiiOptions Pii { get; set; } = new();
    public MaterializationOptions Materialization { get; set; } = new();

    /// <summary>
    /// Local-instance Infisical secret-manager configuration. When
    /// <see cref="InfisicalOptions.Enabled"/> is true, the host bootstraps
    /// secrets (database connection string, Claude API key, JWT signing key,
    /// PII data-protection key) from the running Infisical instance instead
    /// of from <c>appsettings.json</c> / environment variables.
    /// </summary>
    public InfisicalOptions Infisical { get; set; } = new();
}

public sealed class AuthOptions
{
    /// <summary>Local or ActiveDirectory — toggles the entire auth pipeline.</summary>
    public AuthenticationMode Mode { get; set; } = AuthenticationMode.Local;
    public JwtOptions Jwt { get; set; } = new();
    public AzureAdOptions AzureAd { get; set; } = new();
}

public sealed class JwtOptions
{
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 480;
}

public sealed class AzureAdOptions
{
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string Instance { get; set; } = "https://login.microsoftonline.com/";
}

public sealed class CorsOptions
{
    /// <summary>
    /// Origins that are allowed to call the API. Set to the UI host(s).
    /// In production, set via Brusca:Cors:AllowedOrigins in appsettings.Production.json
    /// or the Brusca__Cors__AllowedOrigins__0 environment variable.
    /// </summary>
    public string[] AllowedOrigins { get; set; } = ["http://localhost:4321"];
}

public sealed class LoggingOptions
{
    public LogSinkOptions Audit { get; set; } = new();
    public LogSinkOptions Error { get; set; } = new();
}

public sealed class LogSinkOptions
{
    /// <summary>Database | File | Both | Elasticsearch</summary>
    public LogSinkTarget Sink { get; set; } = LogSinkTarget.Both;
    public string MinimumLevel { get; set; } = "Information";
    public string? FilePath { get; set; }
    public string? ElasticsearchUri { get; set; }
    public string? ElasticsearchIndexFormat { get; set; }
}

public sealed class ClaudeOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "claude-opus-4-5";
    public int MaxTokens { get; set; } = 4096;
}

public sealed class FileSystemOptions
{
    public int MaxDepth { get; set; } = 20;
    public long MaxFileSizeBytes { get; set; } = 10_485_760; // 10 MB
    public string[] IgnoredDirectories { get; set; } =
        [".git", "node_modules", "bin", "obj", ".vs", "__pycache__"];
}

/// <summary>
/// PII redaction + encryption configuration.
///
/// Every file read by a supported reader is passed through the redactor BEFORE
/// any data leaves the host process. The original PII is then sealed into the
/// <c>EncryptedPiiJson</c> column via <c>IEncryptionService</c> and is only
/// decrypted at execution time when materializing target paths.
/// </summary>
public sealed class PiiOptions
{
    /// <summary>Master switch. When false the system fails closed — Claude is NEVER called.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Application name used by ASP.NET Core Data Protection to derive the key.
    /// Change this only if you intend to rotate / partition encryption keys.
    /// </summary>
    public string DataProtectionApplicationName { get; set; } = "Brusca.Pii";

    /// <summary>
    /// Optional override directory for the Data Protection key ring. When null,
    /// the OS default is used (DPAPI on Windows; key file on Linux).
    /// </summary>
    public string? KeyRingDirectory { get; set; }

    /// <summary>Which detectors are active. Disable selectively for tests / regions.</summary>
    public PiiKindToggles Detectors { get; set; } = new();

    /// <summary>Additional regex rules contributed by the deployment.</summary>
    public CustomPiiRule[] CustomRules { get; set; } = [];

    /// <summary>If true, the redacted content sent to Claude is also length-truncated.</summary>
    public int MaxRedactedContentChars { get; set; } = 4000;

    /// <summary>
    /// When true, image files (<c>.jpg .jpeg .png .gif .heic .heif .avif .psd</c>)
    /// are passed through OCR so embedded text is extracted, redacted, and
    /// classified before any data reaches Claude.
    /// </summary>
    public bool ImageOcrEnabled { get; set; } = true;

    /// <summary>
    /// ISO-639 codes (Tesseract format) used by the OCR pass, e.g. <c>"eng"</c>
    /// or <c>"eng+fra"</c>. Multiple languages may be combined with <c>+</c>.
    /// </summary>
    public string OcrLanguages { get; set; } = "eng";

    /// <summary>
    /// Path to the OCR engine's trained-data directory. When null, the
    /// implementation default (e.g. <c>./tessdata</c>) is used.
    /// </summary>
    public string? OcrDataPath { get; set; }
}

public sealed class PiiKindToggles
{
    public bool PersonName { get; set; } = true;
    public bool EmailAddress { get; set; } = true;
    public bool PhoneNumber { get; set; } = true;
    public bool SocialSecurityNumber { get; set; } = true;
    public bool CreditCardNumber { get; set; } = true;
    public bool BankAccountNumber { get; set; } = true;
    public bool DateOfBirth { get; set; } = true;
    public bool StreetAddress { get; set; } = true;
    public bool IpAddress { get; set; } = true;
    public bool DriversLicense { get; set; } = true;
    public bool PassportNumber { get; set; } = true;
    public bool TaxId { get; set; } = true;
    public bool MedicalRecordNumber { get; set; } = true;
    public bool VehicleIdentificationNumber { get; set; } = true;
}

public sealed class CustomPiiRule
{
    public string Name { get; set; } = string.Empty;
    public string RegexPattern { get; set; } = string.Empty;
    /// <summary>Maps to a <see cref="Brusca.Core.Enums.PiiKind"/>; defaults to Custom.</summary>
    public string Kind { get; set; } = "Custom";
}

/// <summary>
/// Knobs that govern how the structure-execution pipeline materializes copies
/// onto the execution target.
/// </summary>
public sealed class MaterializationOptions
{
    /// <summary>
    /// What to do when the templated destination path already exists. Defaults
    /// to <see cref="MaterializationCollisionPolicy.Suffix"/> which appends an
    /// ascending <c>_(2)</c>, <c>_(3)</c> until a free name is found.
    /// </summary>
    public MaterializationCollisionPolicy CollisionPolicy { get; set; } = MaterializationCollisionPolicy.Suffix;

    /// <summary>
    /// When true, the duplicate-detection pass runs before materialization and
    /// every group of byte-identical files materializes only its keeper —
    /// the rest are recorded as <c>SkipDuplicate</c>.
    /// </summary>
    public bool DeduplicateByContentHash { get; set; } = true;

    /// <summary>
    /// Strategy used to pick the keeper inside a duplicate group.
    /// </summary>
    public DuplicateKeepStrategy DuplicateKeepStrategy { get; set; } = DuplicateKeepStrategy.KeepFirstPath;

    /// <summary>
    /// When true, every materialized image/PDF/Office document is run through
    /// <c>IFileMetadataStripper</c> after copy/redact so identifying metadata
    /// (EXIF/XMP, PDF /Info+/Metadata, OpenXml core/extended/custom properties)
    /// is removed from the redacted copy. Originals are never touched.
    /// </summary>
    public bool StripMetadata { get; set; } = true;

    /// <summary>
    /// When true, image files with computed <c>ImageRedactionRegionsJson</c>
    /// regions are materialized via <c>IImageRedactionService</c> instead of
    /// <c>File.Copy</c>, so PII regions are occluded in the destination.
    /// Has no effect when no image-redactor is registered.
    /// </summary>
    public bool SanitizeImages { get; set; } = true;
}

/// <summary>
/// Configuration for a local-instance Infisical secret manager.
///
/// Infisical (https://infisical.com) is run locally in Docker; the API host
/// authenticates with a Universal Auth machine identity and pulls secrets at
/// startup. Values resolved from Infisical override anything found in
/// appsettings.json so the same config file can be used for every environment.
/// </summary>
public sealed class InfisicalOptions
{
    /// <summary>Master switch. When false, all values come from configuration as before.</summary>
    public bool Enabled { get; set; }

    /// <summary>Base URL of the local Infisical instance (e.g. <c>http://localhost:8080</c>).</summary>
    public string SiteUrl { get; set; } = "http://localhost:8080";

    /// <summary>Universal Auth client id for the Brusca machine identity.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Universal Auth client secret. SHOULD be supplied via the
    /// <c>BRUSCA__INFISICAL__CLIENTSECRET</c> environment variable rather
    /// than committed to source control.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>Project (workspace) slug or id holding the Brusca secrets.</summary>
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>Environment slug inside the project (<c>dev</c>, <c>staging</c>, <c>prod</c>).</summary>
    public string Environment { get; set; } = "dev";

    /// <summary>Folder path inside the environment (default <c>/</c>).</summary>
    public string SecretPath { get; set; } = "/";

    /// <summary>How often the host re-pulls secrets from Infisical.</summary>
    public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromMinutes(5);
}
