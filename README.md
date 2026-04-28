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


---

## PII redaction & structure planning (NEW)

Brusca now strips PII from every file BEFORE any data is sent to Claude, encrypts the original PII at rest, and applies a Claude-designed directory layout to either the source path or an alternate path \u2014 recording the before/after of every operation for full audit traceability.

### Pipeline at a glance

1. **Read** \u2014 a supported reader pulls file content into memory. Image
   formats (`.jpg`, `.png`, `.heic`, `.psd`, \u2026) go through `IOcrService`
   first so embedded text is recognized.
2. **Redact** \u2014 `IPiiRedactionService` replaces every PII span with a stable token (`[[PII:Kind:NNNN]]`).
3. **Classify** \u2014 `IDocumentTypeClassifier` assigns a `DocumentType` from the redacted text + extension.
4. **Encrypt** \u2014 `IEncryptionService` (ASP.NET Core Data Protection) seals the PII JSON into a database column.
5. **Plan** \u2014 `IClaudeStructureService` calls Claude with ONLY `DocumentType` + extension counts; receives a `DirectoryStructurePlan` of folder/file templates.
6. **Rehydrate & Execute** \u2014 `IStructureExecutionService` calls
   `IPiiRehydrationService` to fill template tokens from the encrypted
   PII column, hashes each file via `IFileHashService` before/after the
   move, sanitizes any image whose visual content held PII via
   `IImageRedactionService`, performs the move/rename/create, and
   records every before/after path into `cleaning.FileRelocation`.
7. **Undo (optional)** \u2014 `IStructureExecutionService.RollbackAsync`
   reverses every succeeded relocation back to its source path before
   the cleaning is archived.

### Three new database tables

- `cleaning.RedactedFile`  \u2014 redacted descriptor + encrypted PII column.
- `cleaning.StructurePlan` \u2014 Claude-generated layout.
- `cleaning.FileRelocation`\u2014 before/after audit log.

DDL is in `Brusca.Core/docs/DeveloperGuide.md`.

### Working tables vs. archive tables

Brusca enforces a **one-active-cleaning** invariant. Only the currently
in-flight Cleaning lives in the working schema (`cleaning.*`). When a
Cleaning reaches a terminal state (`Completed`, `Failed`, or `Cancelled`)
the API archives it: every row in `cleaning.Cleaning`, `cleaning.RedactedFile`,
`cleaning.StructurePlan`, `cleaning.FileRelocation`, `cleaning.PromptStep`,
`cleaning.PromptStepCommand`, and `cleaning.CleaningFileExtension` is copied
into a mirror table under the `archive.*` schema (preserving Ids and
`CreatedAtUtc` timestamps), then deleted from the working tables.

Every persistable record carries a `CreatedAtUtc` (or equivalent
`DiscoveredAtUtc` / `GeneratedAtUtc`) datetime so the archive is fully
reconstructable.

### Recognized file types

Brusca's domain catalog (`Brusca.Core.Models.Extensions.KnownFileExtensions`)
seeds these readers; image formats run through OCR first so embedded text
is also redacted before reaching Claude.

| Category   | Extensions |
|------------|------------|
| Plain text | `.txt`, `.rtf`, `.csv` |
| Word       | `.docx`, `.odt`, `.pages`, `.pdf` |
| Spreadsheet| `.xlsx`, `.ods`, `.numbers` |
| Image (OCR)| `.jpg`, `.jpeg`, `.png`, `.gif`, `.heic`, `.heif`, `.avif`, `.psd` |

### Five new API endpoints

- `POST /api/cleanings/{id}/redact`
- `POST /api/cleanings/{id}/generate-structure`
- `GET  /api/cleanings/{id}/structure-plan`
- `POST /api/cleanings/{id}/execute-structure`
- `GET  /api/cleanings/{id}/relocations`
- `POST /api/cleanings/{id}/archive`  *(moves working rows into `archive.*`)*
- `GET  /api/cleanings/active`        *(returns the single un-archived cleaning, if any)*

Full docs in `Brusca.Api/docs/UserGuide.md`.

---

## Secret management (Infisical)

Production deployments resolve secrets (database connection string, Claude
API key, JWT signing key, PII data-protection key) from a **local-instance
Infisical** server via `BruscaOptions.Infisical` and the `ISecretProvider`
contract.

Step-by-step setup is in [`docs/SetupSteps.md`](docs/SetupSteps.md).

---

## Documentation index

| Topic | Location |
|-------|----------|
| Step-by-step install | `docs/InstallGuide.md` |
| End-user / integrator guide | `docs/UserGuide.md` |
| Engineer / contributor guide | `docs/DeveloperGuide.md` |
| Local Infisical setup | `docs/SetupSteps.md` |

For the cross-repo pipeline overview, start in `Brusca.Core/docs/UserGuide.md` then walk down through `Brusca.Infrastructure`, `Brusca.Api`, `Brusca.Web`, and `Brusca.Tests`.
