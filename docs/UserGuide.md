# User Guide — Brusca.Core

`Brusca.Core` is a library, not a runnable application. End-users never interact with it directly. They interact with **Brusca.Web** (the UI), which calls **Brusca.Api**, which uses **Brusca.Infrastructure**, which depends on `Brusca.Core`.

This page is therefore for **integrators** who consume `Brusca.Core` from another solution.

---

## 1. What lives in `Brusca.Core`

| Folder | Contents |
|--------|----------|
| `Models/Cleaning/`   | `Cleaning`, `CleaningPromptStep`, `PromptStepCommand`, `DirectoryNode`, `TreeComparisonResult`, `DirectoryStructurePlan`, `DirectoryStructureRule`, `FileRelocationRecord` |
| `Models/Extensions/` | `FileExtensionRecord`, `ExtensionScanResult`, `KnownFileExtensions` |
| `Models/Pii/`        | `PiiSegment`, `RedactedFileDescriptor`, `DocumentTypeSummary` |
| `Models/Logging/`    | `AuditLogEntry`, `ErrorLogEntry` |
| `Models/`            | `BruscaOptions` (typed-options root) including `PiiOptions` |
| `Contracts/Services/`     | `ICleaningService`, `IFileSystemService`, `IFileExtensionService`, `ITreeProjectionService`, `IPiiRedactionService`, `IDocumentTypeClassifier`, `IEncryptionService`, `IClaudeStructureService`, `IStructureExecutionService`, `IOcrService`, `ISecretProvider` |
| `Contracts/Repositories/` | `ICleaningRepository`, `IFileExtensionRepository`, `IPromptStepRepository`, `IPromptStepCommandRepository`, `IRedactedFileRepository`, `IStructurePlanRepository`, `IFileRelocationRepository` |
| `Contracts/Logging/`      | `IAuditLogger`, `IErrorLogger` |
| `Enums/`                  | All enums including `DocumentType`, `PiiKind`, `RelocationOperationType`, `RelocationStatus`, and the new `CleaningStatus` values (`Redacting`, `Redacted`, `StructurePlanGenerated`, `StructureExecuting`, `Archived`) |

---

## 1a. Recognized file types

Brusca's domain catalog ships a default reader strategy for the file
formats below. Image formats run through OCR first so embedded text is
also redacted before reaching Claude.

| Mode                | Extensions                                                          |
|---------------------|---------------------------------------------------------------------|
| Text                | `.txt`, `.csv`                                                      |
| Structured document | `.rtf`, `.docx`, `.odt`, `.pages`, `.pdf`                           |
| Spreadsheet         | `.xlsx`, `.ods`, `.numbers`                                         |
| OCR (images)        | `.jpg`, `.jpeg`, `.png`, `.gif`, `.heic`, `.heif`, `.avif`, `.psd`  |

The catalog is exposed at compile time via `KnownFileExtensions.All`,
`KnownFileExtensions.OcrImageExtensions`, and `KnownFileExtensions.RequiresOcr(ext)`.

---

## 1b. Active vs archived cleanings

Brusca keeps **only one Cleaning at a time** in the working tables. When
a Cleaning finishes, the API moves every row (Cleaning, file extensions,
prompt steps + commands, redacted descriptors, structure plans, file
relocations) into a parallel `archive.*` schema, preserving every Id and
`CreatedAtUtc` timestamp. Use `ICleaningService.GetActiveCleaningAsync`
to discover whether a fresh cleaning may be started.

---

## 2. The PII-aware processing pipeline

`Brusca.Core` defines the **contract** of the pipeline; the implementations live in `Brusca.Infrastructure`.

```
        ┌─────────────────────┐
input → │ IPiiRedactionService│ ─► redacted text + PiiSegment[] (in memory)
        └─────────────────────┘
                  │
                  ▼
        ┌─────────────────────────┐
        │ IDocumentTypeClassifier │ ─► DocumentType
        └─────────────────────────┘
                  │
                  ▼
        ┌─────────────────────┐
        │ IEncryptionService  │ ─► EncryptedPiiJson  (sealed at rest)
        └─────────────────────┘
                  │  (RedactedFileDescriptor persisted)
                  ▼
        ┌──────────────────────────┐
        │ IClaudeStructureService  │ ◄── DocumentTypeSummary[]  (no PII)
        └──────────────────────────┘
                  │  DirectoryStructurePlan
                  ▼
        ┌──────────────────────────┐
        │ IStructureExecutionService│ ─► FileRelocationRecord[] (before/after)
        └──────────────────────────┘
```

### Guarantees

1. The original PII string is **only** held in memory inside `PiiSegment.Value` for the duration of one redaction pass.
2. At rest the PII lives **only** in `RedactedFileDescriptor.EncryptedPiiJson`, encrypted by `IEncryptionService`.
3. The Claude structure call (`IClaudeStructureService.AnalyzeStructureAsync`) receives ONLY `DocumentTypeSummary` rows — no file names, no content, no PII.
4. Every move/rename/create operation is logged as a `FileRelocationRecord` capturing both before-state and after-state (path + name).

---

## 3. Configuration shape

`BruscaOptions` is bound from `appsettings.json` at the `Brusca` root. The new sub-section is `Pii`:

```json
"Brusca": {
  "Pii": {
    "Enabled": true,
    "DataProtectionApplicationName": "Brusca.Pii",
    "KeyRingDirectory": null,
    "MaxRedactedContentChars": 4000,
    "ImageOcrEnabled": true,
    "OcrLanguages": "eng",
    "OcrDataPath": null,
    "Detectors": {
      "PersonName": true, "EmailAddress": true, "PhoneNumber": true,
      "SocialSecurityNumber": true, "CreditCardNumber": true,
      "BankAccountNumber": true, "DateOfBirth": true,
      "StreetAddress": true, "IpAddress": true,
      "DriversLicense": true, "PassportNumber": true,
      "TaxId": true, "MedicalRecordNumber": true,
      "VehicleIdentificationNumber": true
    },
    "CustomRules": [
      { "Name": "EmployeeId", "RegexPattern": "EMP-\\d{6}", "Kind": "Custom" }
    ]
  }
}
```

---

## 4. Where to learn more

- API surface: `Brusca.Api/docs/UserGuide.md`
- End-user UI walkthrough: `Brusca.Web/docs/UserGuide.md`
- Build / contribute: `docs/DeveloperGuide.md` in this repo
