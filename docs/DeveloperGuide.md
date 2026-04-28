# Developer Guide — Brusca.Core

For engineers extending the domain kernel.

---

## 1. The dependency rule

> **Nothing in `Brusca.Core` may reference a concrete implementation.**

Allowed package references:

- `Microsoft.Extensions.Logging.Abstractions`
- `Microsoft.Extensions.DependencyInjection.Abstractions`
- `Microsoft.Extensions.Options`
- `Audit.NET` (abstraction only)
- `FluentResults`
- `Ardalis.GuardClauses`

Anything else (Dapper, Serilog, the Anthropic SDK, ASP.NET Core Data Protection, etc.) **belongs in `Brusca.Infrastructure`**.

---

## 2. Adding a new contract

When you add a new interface to `Contracts/Services` or `Contracts/Repositories`:

1. Define it in `Brusca.Core`.
2. Add a corresponding implementation in `Brusca.Infrastructure`.
3. Register it in `Brusca.Infrastructure/Configuration/InfrastructureRegistration.cs`.
4. Repack `Brusca.Core` (see `InstallGuide.md`).
5. Bump the package version pin in every consumer (`Brusca.Infrastructure`, `Brusca.Api`, `Brusca.Tests`).
6. Repack `Brusca.Infrastructure` and re-restore consumers.

> **Tip:** during active development pin a single version like `1.0.999-pii` across all repos. When you cut a release, bump the production version.

---

## 3. The PII pipeline contract

The PII flow is split across three small interfaces so each piece can be replaced independently:

| Interface | Responsibility |
|-----------|----------------|
| `IPiiRedactionService`     | Detect PII spans, return redacted text + segments (PII still in memory only) |
| `IDocumentTypeClassifier`  | Map redacted content + extension → `DocumentType` |
| `IEncryptionService`       | Symmetric encryption of the PII JSON column |
| `IClaudeStructureService`  | Anonymized call to Claude (DocumentType + extension counts only) |
| `IStructureExecutionService` | Apply the plan, record before/after `FileRelocationRecord` rows |

### When adding a new `PiiKind`

1. Add the enum value to `Brusca.Core.Enums.PiiKind`.
2. Add a toggle to `PiiKindToggles` in `Models/Options.cs` (default `true` if it is broadly applicable, otherwise `false`).
3. Implement detection in `Brusca.Infrastructure/Pii/RegexPiiRedactionService.cs` (or a new specialized service).
4. Add tests under `Brusca.Tests/Infrastructure/RegexPiiRedactionServiceTests.cs`.

### When adding a new `DocumentType`

1. Add the enum value to `Brusca.Core.Enums.DocumentType`.
2. Update extension and keyword maps in `HeuristicDocumentTypeClassifier`.
3. Update the structure-plan prompt in `ClaudeStructureService` if the doc type benefits from a hint.
4. Add tests under `Brusca.Tests/Infrastructure/HeuristicDocumentTypeClassifierTests.cs`.

---

## 4. The `Cleaning` state machine

```
Pending
  │ /scan
  ▼
Scanning ─► AwaitingExtensionResolution (if unknown extensions)
  │                │ /restart
  │                ▼
  │           Restarted ─► Pending
  ▼
Analyzing
  │ /redact            (NEW)
  ▼
Redacting ─► Redacted
  │ /generate-structure (NEW)
  ▼
StructurePlanGenerated
  │ /execute-structure  (NEW)
  ▼
StructureExecuting ─► Completed | Failed
```

The legacy prompt-step pipeline (`/generate-steps` → `/execute`) is preserved for backwards compatibility but the recommended path is the redaction-aware flow above.

---

## 5. Database schema additions required

The new repositories call these stored procedures (schema `cleaning`):

| Stored procedure | Repository |
|------------------|------------|
| `cleaning.usp_RedactedFile_Insert`                         | `IRedactedFileRepository.CreateAsync` |
| `cleaning.usp_RedactedFile_GetById`                        | `IRedactedFileRepository.GetByIdAsync` |
| `cleaning.usp_RedactedFile_GetByCleaningId`                | `IRedactedFileRepository.GetByCleaningIdAsync` |
| `cleaning.usp_RedactedFile_GetDocumentTypeSummaries`       | `IRedactedFileRepository.GetDocumentTypeSummariesAsync` |
| `cleaning.usp_RedactedFile_DeleteByCleaningId`             | `IRedactedFileRepository.DeleteByCleaningIdAsync` |
| `cleaning.usp_StructurePlan_Insert`                        | `IStructurePlanRepository.CreateAsync` |
| `cleaning.usp_StructurePlan_GetLatest`                     | `IStructurePlanRepository.GetLatestAsync` |
| `cleaning.usp_StructurePlan_DeleteByCleaningId`            | `IStructurePlanRepository.DeleteByCleaningIdAsync` |
| `cleaning.usp_FileRelocation_Insert`                       | `IFileRelocationRepository.CreateAsync` |
| `cleaning.usp_FileRelocation_UpdateStatus`                 | `IFileRelocationRepository.UpdateStatusAsync` |
| `cleaning.usp_FileRelocation_GetByCleaningId`              | `IFileRelocationRepository.GetByCleaningIdAsync` |

Required tables:

```sql
CREATE TABLE cleaning.RedactedFile (
    Id                  uniqueidentifier NOT NULL PRIMARY KEY,
    CleaningId          uniqueidentifier NOT NULL,
    OriginalFilePath    nvarchar(1024)   NOT NULL,
    OriginalFileName    nvarchar(512)    NOT NULL,
    Extension           nvarchar(32)     NOT NULL,
    DocumentType        int              NOT NULL,
    RedactedContent     nvarchar(max)    NULL,
    EncryptedPiiJson    nvarchar(max)    NULL,
    PiiSegmentCount     int              NOT NULL,
    ContentHash         char(64)         NULL,
    DiscoveredAtUtc     datetime2        NOT NULL
);

CREATE TABLE cleaning.StructurePlan (
    Id              uniqueidentifier NOT NULL PRIMARY KEY,
    CleaningId      uniqueidentifier NOT NULL,
    Summary         nvarchar(2000)   NULL,
    RulesJson       nvarchar(max)    NOT NULL,
    RawPlanJson     nvarchar(max)    NULL,
    GeneratedAtUtc  datetime2        NOT NULL
);

CREATE TABLE cleaning.FileRelocation (
    Id              uniqueidentifier NOT NULL PRIMARY KEY,
    CleaningId      uniqueidentifier NOT NULL,
    RedactedFileId  uniqueidentifier NULL,
    OperationType   int              NOT NULL,
    ExecutionTarget int              NOT NULL,
    BeforePath      nvarchar(1024)   NULL,
    BeforeName      nvarchar(512)    NULL,
    AfterPath       nvarchar(1024)   NULL,
    AfterName       nvarchar(512)    NULL,
    Status          int              NOT NULL,
    ErrorMessage    nvarchar(2000)   NULL,
    CreatedAtUtc    datetime2        NOT NULL,
    CompletedAtUtc  datetime2        NULL
);
```

Stored-procedure scripts live alongside the existing `cleaning.usp_Cleaning_*` set in your DB-migrations repo or under `Brusca.Api/sql/` (depending on how your team chooses to manage them).

---

## 5b. Working vs archive schema

Brusca operates on a **one-active-cleaning** model. Only the in-flight
Cleaning lives in `cleaning.*`. As soon as a Cleaning reaches a terminal
state (`Completed`, `Failed`, or `Cancelled`) the API calls
`ICleaningService.ArchiveCleaningAsync`, which through
`ICleaningRepository.ArchiveAsync` performs a transactional move into the
`archive.*` schema and deletes the originals.

Every working table has a 1:1 mirror under `archive.*` with the **same**
columns plus an `ArchivedAtUtc datetime2 NOT NULL` column. `CreatedAtUtc`
(or the table's equivalent — `DiscoveredAtUtc`, `GeneratedAtUtc`) is
preserved verbatim so the archive can be replayed if needed.

```sql
CREATE SCHEMA archive;

CREATE TABLE archive.Cleaning              (LIKE cleaning.Cleaning              INCLUDING ALL, ArchivedAtUtc datetime2 NOT NULL);
CREATE TABLE archive.CleaningFileExtension (LIKE cleaning.CleaningFileExtension INCLUDING ALL, ArchivedAtUtc datetime2 NOT NULL);
CREATE TABLE archive.PromptStep            (LIKE cleaning.PromptStep            INCLUDING ALL, ArchivedAtUtc datetime2 NOT NULL);
CREATE TABLE archive.PromptStepCommand     (LIKE cleaning.PromptStepCommand     INCLUDING ALL, ArchivedAtUtc datetime2 NOT NULL);
CREATE TABLE archive.RedactedFile          (LIKE cleaning.RedactedFile          INCLUDING ALL, ArchivedAtUtc datetime2 NOT NULL);
CREATE TABLE archive.StructurePlan         (LIKE cleaning.StructurePlan         INCLUDING ALL, ArchivedAtUtc datetime2 NOT NULL);
CREATE TABLE archive.FileRelocation        (LIKE cleaning.FileRelocation        INCLUDING ALL, ArchivedAtUtc datetime2 NOT NULL);
```

> The `LIKE ... INCLUDING ALL` syntax above is illustrative — for SQL
> Server, replicate each column list explicitly. The key requirement is
> that no archived row mutates `Id` or `CreatedAtUtc`.

Stored procedures (all transactional):

| Stored procedure                          | Repository |
|-------------------------------------------|------------|
| `cleaning.usp_Cleaning_GetActive`         | `ICleaningRepository.GetActiveAsync` |
| `cleaning.usp_Cleaning_Archive`           | `ICleaningRepository.ArchiveAsync` |
| `archive.usp_Cleaning_GetById`            | `ICleaningRepository.GetArchivedByIdAsync` |
| `archive.usp_Cleaning_GetPaged`           | `ICleaningRepository.GetArchivedPagedAsync` |

`cleaning.usp_Cleaning_Archive` MUST in a single transaction:

1. Insert the matching rows into every `archive.*` mirror (with
   `ArchivedAtUtc = SYSUTCDATETIME()`).
2. Update the archived `Cleaning.Status` to `14 /* Archived */`.
3. Delete the working rows in dependency order.
4. Commit.

---

## 5c. Recognized file types

`Brusca.Core.Models.Extensions.KnownFileExtensions` is the authoritative
seed list of extensions Brusca is expected to read:

| Mode                | Extensions                                              |
|---------------------|---------------------------------------------------------|
| Text                | `.txt`, `.csv`                                          |
| StructuredDocument  | `.rtf`, `.docx`, `.odt`, `.pages`, `.pdf`               |
| Spreadsheet         | `.xlsx`, `.ods`, `.numbers`                             |
| Ocr                 | `.jpg`, `.jpeg`, `.png`, `.gif`, `.heic`, `.heif`, `.avif`, `.psd` |

The OCR extensions go through `IOcrService.ExtractTextAsync` first so
embedded text is recognized, then handed to `IPiiRedactionService`. No
image bytes ever leave the host — only the redacted `DocumentType` +
extension counts reach Claude.

Adding a new extension:

1. Append a `KnownExtension` record to `KnownFileExtensions.All`.
2. Implement (or extend) the matching reader in `Brusca.Infrastructure`.
3. If it is a new image format, register it with `IOcrService`.
4. Repack `Brusca.Core` and bump the consumer pin.

---

## 5d. Secret management (Infisical)

`ISecretProvider` is the abstraction over runtime secrets. The default
infrastructure implementation reads from a local-instance Infisical
server when `BruscaOptions.Infisical.Enabled` is true, otherwise from
`IConfiguration` (preserving the existing dev experience).

Secret keys mirror the configuration shape:

| Configuration path                       | Infisical key                        |
|------------------------------------------|--------------------------------------|
| `Brusca:DatabaseConnectionString`        | `DatabaseConnectionString`           |
| `Brusca:Claude:ApiKey`                   | `Claude:ApiKey`                      |
| `Brusca:Auth:Jwt:SecretKey`              | `Auth:Jwt:SecretKey`                 |
| `Brusca:Pii:DataProtectionApplicationName` | `Pii:DataProtectionApplicationName`|

End-to-end provisioning steps live in `docs/SetupSteps.md`.

---

## 6. Coding conventions

- C# 13 / .NET 9 features welcome (collection expressions, file-scoped namespaces, primary constructors).
- All async methods accept a `CancellationToken ct = default`.
- Repository contracts return `FluentResults.Result<T>` — never throw across boundaries.
- New options classes live under `Brusca.Core.Models` and are reachable from `BruscaOptions`.
- Keep XML doc comments on public APIs — `GenerateDocumentationFile` is enabled.

---

## 7. Releasing

`/.github/workflows/release.yml` runs on push of a `v*` tag and:

1. Packs with the version derived from the tag.
2. Pushes to GitHub Packages.
3. Pushes to NuGet.org when `NUGET_API_KEY` is set.

Always tag from `main` only after the four downstream repos have green CI against the new version.
