# Install Guide — Brusca.Core

This guide gets `Brusca.Core` building, packing, and consumable from the four sibling repos.

> `Brusca.Core` is a **pure domain library**. It contains zero infrastructure dependencies and is published as the `Brusca.Core` NuGet package.

---

## 1. Prerequisites

| Tool | Version |
|------|---------|
| .NET SDK | 9.0+ |
| Git | any |
| (optional) Visual Studio | 2026 (18.4+) or VS Code |

A shared local NuGet feed at `..\nupkgs` (one level above all five Brusca repos) is used during development. Sibling repos consume this feed via their `nuget.config`.

```
\OOBT-NAS\Workstation\Repo\
├── Brusca.Core/
├── Brusca.Api/
├── Brusca.Infrastructure/
├── Brusca.Tests/
├── Brusca.Web/
└── nupkgs/        ← shared local feed
```

---

## 2. Clone and restore

```powershell
cd \\OOBT-NAS\Workstation\Repo
git clone https://github.com/Out-Of-Box-Thinking/Brusca.Core.git
cd Brusca.Core
dotnet restore Brusca.Core/Brusca.Core.csproj
```

If the restore fails because `..\nupkgs` does not exist, create it once:

```powershell
New-Item -ItemType Directory -Path ..\nupkgs -Force
```

---

## 3. Build

```powershell
dotnet build Brusca.Core/Brusca.Core.csproj -c Debug
```

Expected output: `Build succeeded. 0 Error(s).` (warnings about XML doc comments are non-fatal.)

---

## 4. Pack to the local feed

After every change to public types in `Brusca.Core`, repack so the four downstream repos pick up the new contract:

```powershell
.\pack.ps1 -Version 1.0.999-pii
# or:
dotnet pack Brusca.Core/Brusca.Core.csproj -c Debug -o ..\nupkgs /p:Version=1.0.999-pii
```

If you have already restored a prior `1.0.999-pii` package, clear the cache before rebuilding the consumers:

```powershell
Remove-Item -Recurse -Force "$env:USERPROFILE\.nuget\packages\brusca.core" -ErrorAction SilentlyContinue
```

---

## 5. Verify the package

```powershell
$pkg  = Get-ChildItem ..\nupkgs\Brusca.Core.*.nupkg | Sort-Object LastWriteTime -Desc | Select -First 1
$tmp  = Join-Path $env:TEMP "bcore_$(New-Guid)"
Expand-Archive $pkg.FullName $tmp -Force
[System.Reflection.Assembly]::LoadFrom("$tmp\lib\net9.0\Brusca.Core.dll").GetTypes() |
    Where-Object { $_.Name -match 'PiiRedaction|RedactedFile|StructurePlan' } |
    Select-Object FullName
```

You should see types from the new PII pipeline:

- `Brusca.Core.Contracts.Services.IPiiRedactionService`
- `Brusca.Core.Contracts.Services.IDocumentTypeClassifier`
- `Brusca.Core.Contracts.Services.IEncryptionService`
- `Brusca.Core.Contracts.Services.IClaudeStructureService`
- `Brusca.Core.Contracts.Services.IStructureExecutionService`
- `Brusca.Core.Contracts.Repositories.IRedactedFileRepository`
- `Brusca.Core.Contracts.Repositories.IStructurePlanRepository`
- `Brusca.Core.Contracts.Repositories.IFileRelocationRepository`
- `Brusca.Core.Models.Pii.PiiSegment`
- `Brusca.Core.Models.Pii.RedactedFileDescriptor`
- `Brusca.Core.Models.Cleaning.DirectoryStructurePlan`
- `Brusca.Core.Models.Cleaning.FileRelocationRecord`

---

## 6. Continue with downstream repos

Follow `Brusca.Infrastructure/docs/InstallGuide.md` next, then `Brusca.Api`, `Brusca.Tests`, and `Brusca.Web`.

---

## 7. Local-instance Infisical (secret manager)

Before running `Brusca.Api` against a real database or Claude key, stand up
the local Infisical instance and seed the Brusca secrets — full step-by-step
in [`docs/SetupSteps.md`](SetupSteps.md). Brusca will refuse to start with
`Brusca:Infisical:Enabled = true` until the machine identity, project, and
required keys exist.
