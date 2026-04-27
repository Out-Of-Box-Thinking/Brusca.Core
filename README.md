# Brusca.Core

The domain kernel of the Brusca AI-powered file organizer. This library contains **zero concrete dependencies** — only models, interfaces, enums, and configuration options. All other Brusca components depend on this package.

---

## What lives here

| Folder | Contents |
|--------|----------|
| `Models/Cleaning/` | `Cleaning`, `PromptStep`, `PromptStepCommand` domain models |
| `Models/Extensions/` | `FileExtension` model |
| `Models/Logging/` | `ErrorLog`, `AuditLog` models |
| `Models/` | `BruscaOptions` — the single typed-options root |
| `Contracts/Repositories/` | `ICleaningRepository`, `IFileExtensionRepository`, `IPromptStepRepository`, `IPromptStepCommandRepository` |
| `Contracts/Services/` | `ICleaningService`, `IFileExtensionService`, `IFileSystemService`, `IClaudePromptService` |
| `Contracts/Logging/` | `IErrorLogger`, `IAuditLogger` |
| `Enums/` | All domain enumerations |

---

## Dependency rule

> **Nothing in `Brusca.Core` may reference a concrete implementation.**

Allowed references:
- `Microsoft.Extensions.Logging.Abstractions`
- `Microsoft.Extensions.DependencyInjection.Abstractions`
- `Microsoft.Extensions.Options`
- `Audit.NET` (abstraction only)
- `FluentResults`
- `Ardalis.GuardClauses`

---

## Versioning & NuGet

This library is published as the **`Brusca.Core`** NuGet package. Version follows [Semantic Versioning](https://semver.org/).

### Pack locally for development

A shared local feed at `../nupkgs` (one level above all Brusca repos) is used during development.

```powershell
# From the repo root
.\pack.ps1 -Version 1.0.0
```

All dependent repos (`Brusca.Infrastructure`, `Brusca.Api`, `Brusca.Tests`) reference this feed via their `nuget.config`.

### Publish to a remote feed (CI/CD)

```powershell
dotnet pack Brusca.Core/Brusca.Core.csproj -c Release /p:Version=${{ version }}
dotnet nuget push nupkgs/Brusca.Core.*.nupkg --source <feed-url> --api-key <key>
```

---

## Build

```bash
dotnet restore
dotnet build
```

---

## Target framework

`.NET 9` — `net9.0`

---

## Related repositories

| Repo | Role |
|------|------|
| [Brusca.Infrastructure](../Brusca.Infrastructure) | Implements all Core interfaces |
| [Brusca.Api](../Brusca.Api) | ASP.NET Core 9 host, REST API |
| [Brusca.Tests](../Brusca.Tests) | xUnit integration and unit tests |
