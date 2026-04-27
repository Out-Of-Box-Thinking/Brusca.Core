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

## Continuous integration

| Workflow | Trigger | What it does |
|----------|---------|--------------|
| [`.github/workflows/ci.yml`](.github/workflows/ci.yml) | Push or PR to `main` | `dotnet restore` → `dotnet build` → `dotnet pack` → uploads `*.nupkg` artifact |
| [`.github/workflows/release.yml`](.github/workflows/release.yml) | Push of a `v*` tag | Packs with version derived from the tag (strips leading `v`) and pushes to **GitHub Packages**; also pushes to **NuGet.org** when the `NUGET_API_KEY` secret is set |

### Cutting a release

```powershell
# Bump <Version> in Brusca.Core/Brusca.Core.csproj first if you want it embedded in the assembly
git tag v1.0.1
git push origin v1.0.1
```

The release workflow uses the built-in `GITHUB_TOKEN` to push to GitHub Packages — no extra setup required. To also publish to NuGet.org, add a repo secret named `NUGET_API_KEY`.

---

## Related repositories

| Repo | Role |
|------|------|
| [Brusca.Infrastructure](https://github.com/Out-Of-Box-Thinking/Brusca.Infrastructure) | Implements all Core interfaces |
| [Brusca.Api](https://github.com/Out-Of-Box-Thinking/Brusca.Api) | ASP.NET Core 9 host, REST API |
| [Brusca.Tests](https://github.com/Out-Of-Box-Thinking/Brusca.Tests) | xUnit integration and unit tests |
| [Brusca.Web](https://github.com/Out-Of-Box-Thinking/Brusca.Web) | Astro 5 front-end |
