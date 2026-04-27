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
