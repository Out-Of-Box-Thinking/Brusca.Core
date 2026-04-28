# Setup Steps — Brusca with a local-instance Infisical

This guide gets a self-hosted **Infisical** instance running on the same
host as Brusca and wires `Brusca.Core` / `Brusca.Api` to read every secret
(database connection string, Claude API key, JWT signing key, PII data
protection key) from it.

> The defaults below assume Windows 11 / Windows Server 2022 with Docker
> Desktop, and a Brusca dev tree at `\\OOBT-NAS\Workstation\Repo\`.
> Linux/macOS commands are equivalent — substitute `docker` paths.

---

## 1. Prerequisites

| Tool | Minimum version | Notes |
|------|-----------------|-------|
| Docker Desktop | 4.30 | Engine + Compose v2 |
| Git | any | |
| .NET SDK | 9.0 | for Brusca |
| PowerShell | 7.4 | preferred shell |
| OpenSSL or `[guid]::NewGuid()` | — | to mint signing secrets |

A spare port pair on `localhost`:

| Service | Port |
|---------|------|
| Infisical web UI / API | `8080` |
| Infisical Postgres | `5433` *(internal — not exposed by default)* |
| Infisical Redis | `6379` *(internal — not exposed by default)* |

---

## 2. Lay out the Infisical stack

Create a sibling directory next to the Brusca repos so the local stack is
not committed into any of them:

```powershell
cd \\OOBT-NAS\Workstation\Repo
New-Item -ItemType Directory -Path .\infisical -Force | Out-Null
cd .\infisical
```

### 2.1 docker-compose.yml

Save the following as `docker-compose.yml`:

```yaml
version: "3.9"

services:
  infisical:
    image: infisical/infisical:latest-postgres
    container_name: brusca-infisical
    restart: unless-stopped
    depends_on:
      - infisical-db
      - infisical-redis
    ports:
      - "8080:8080"
    env_file:
      - .env
    volumes:
      - infisical_data:/var/lib/infisical

  infisical-db:
    image: postgres:16-alpine
    container_name: brusca-infisical-db
    restart: unless-stopped
    environment:
      POSTGRES_USER: infisical
      POSTGRES_PASSWORD: infisical
      POSTGRES_DB: infisical
    volumes:
      - infisical_db:/var/lib/postgresql/data

  infisical-redis:
    image: redis:7-alpine
    container_name: brusca-infisical-redis
    restart: unless-stopped
    volumes:
      - infisical_redis:/data

volumes:
  infisical_data:
  infisical_db:
  infisical_redis:
```

### 2.2 .env

Generate strong keys and save them as `.env` next to the compose file.
Treat this file as a secret — do **not** commit it.

```powershell
# Generate two 32-byte secrets
$encKey  = -join ((48..57)+(65..90)+(97..122) | Get-Random -Count 32 | %{[char]$_})
$authKey = -join ((48..57)+(65..90)+(97..122) | Get-Random -Count 32 | %{[char]$_})

@"
# --- Infisical core ---
ENCRYPTION_KEY=$encKey
AUTH_SECRET=$authKey
SITE_URL=http://localhost:8080

# --- Postgres ---
DB_CONNECTION_URI=postgres://infisical:infisical@infisical-db:5432/infisical

# --- Redis ---
REDIS_URL=redis://infisical-redis:6379

# --- Telemetry off for local ---
TELEMETRY_ENABLED=false
"@ | Out-File -Encoding ascii .env
```

### 2.3 Bring it up

```powershell
docker compose up -d
docker compose logs -f infisical    # wait for "Server started on port 8080"
```

Browse to `http://localhost:8080` — you should see the Infisical first-run page.

---

## 3. First-run admin + project

1. Create the **first admin user** through the web UI (this is forced on a fresh install).
2. Create an **organization** named `Brusca`.
3. Create a **project** named `Brusca` (slug `brusca`).
4. Inside the project the default environments are `dev`, `staging`, `prod`.
   For local development we will populate `dev`.

### 3.1 Add the Brusca secrets to `dev`

Add the following keys (Project → Secrets → environment `dev`, path `/`):

| Key                                  | Sample value                                                  |
|--------------------------------------|---------------------------------------------------------------|
| `DatabaseConnectionString`           | `Server=localhost;Database=Brusca;Trusted_Connection=true;TrustServerCertificate=true;` |
| `Claude:ApiKey`                      | `sk-ant-...`                                                  |
| `Claude:Model`                       | `claude-opus-4-5`                                             |
| `Auth:Jwt:SecretKey`                 | *(64+ char random string)*                                    |
| `Auth:Jwt:Issuer`                    | `Brusca`                                                      |
| `Auth:Jwt:Audience`                  | `Brusca.Api`                                                  |
| `Pii:DataProtectionApplicationName`  | `Brusca.Pii`                                                  |
| `Pii:OcrLanguages`                   | `eng`                                                         |

> Use Infisical's **secret reference** feature for derived values
> (e.g. `Auth:Jwt:Audience` → `${Auth:Jwt:Issuer}.Api`) when convenient.

---

## 4. Create the machine identity for Brusca

Brusca authenticates to Infisical with **Universal Auth** (client id +
client secret), not with a human login.

1. **Organization → Access Control → Identities → Create Identity**
   - Name: `brusca-api`
   - Auth Method: **Universal Auth**
   - Token TTL: `7d` (or shorter; Brusca refreshes automatically)
2. **Identities → brusca-api → Authentication → Create Client Secret**
   - Description: `Local dev`
   - Click **Create** and copy the `Client ID` + `Client Secret` immediately —
     the secret is shown only once.
3. **Project → Access Control → Identities → Add identity**
   - Add `brusca-api` and grant the `Developer` role on environment `dev`
     (or a custom role with `secrets:read` on path `/`).

Persist the credentials on the API host:

```powershell
# Per-user environment variables (PowerShell, persistent)
[Environment]::SetEnvironmentVariable("BRUSCA__INFISICAL__CLIENTID",     "<paste>", "User")
[Environment]::SetEnvironmentVariable("BRUSCA__INFISICAL__CLIENTSECRET", "<paste>", "User")
```

`BRUSCA__INFISICAL__CLIENTSECRET` is the **only** secret that must live
outside Infisical — it is what bootstraps the rest.

---

## 5. Wire Brusca to Infisical

`Brusca.Core` exposes the contract; the implementation lives in
`Brusca.Infrastructure` and is selected automatically when
`Brusca:Infisical:Enabled` is `true`.

### 5.1 appsettings.json

```json
{
  "Brusca": {
    "Infisical": {
      "Enabled": true,
      "SiteUrl": "http://localhost:8080",
      "ProjectId": "brusca",
      "Environment": "dev",
      "SecretPath": "/",
      "RefreshInterval": "00:05:00"
    }
  }
}
```

`ClientId` and `ClientSecret` come from the environment variables set in
section 4 — `BRUSCA__INFISICAL__CLIENTID` /
`BRUSCA__INFISICAL__CLIENTSECRET`.

### 5.2 What happens at startup

1. `Brusca.Api` binds `BruscaOptions.Infisical`.
2. Infrastructure registers an `ISecretProvider` that calls Infisical's
   Universal Auth endpoint with the env-var credentials.
3. Every other secret-bearing option (`Brusca:DatabaseConnectionString`,
   `Brusca:Claude:ApiKey`, `Brusca:Auth:Jwt:SecretKey`, …) is resolved by
   the secret provider — values in `appsettings.json` are ignored when a
   matching key exists in Infisical.
4. A background timer re-pulls secrets every `RefreshInterval` so a
   rotation in Infisical takes effect without restarting the API.

### 5.3 Verify

```powershell
# From Brusca.Api repo
dotnet run --project Brusca.Api/Brusca.Api.csproj
```

You should see one of the following on the first request:

```
info: Brusca.Infrastructure.Secrets.InfisicalSecretProvider[0]
      Loaded 8 secrets from Infisical (project=brusca env=dev path=/)
```

If you see `Falling back to IConfiguration for key '...'`, that key is
missing in Infisical — go back to section 3.1 and add it.

---

## 6. Day-to-day operations

| Task                              | Command |
|-----------------------------------|---------|
| Stop the local instance           | `docker compose stop` |
| Start it again                    | `docker compose start` |
| Tail logs                         | `docker compose logs -f infisical` |
| Back up secrets (export project)  | Project → Settings → **Export `.env`** |
| Rotate the `brusca-api` secret    | Identities → brusca-api → Authentication → Revoke + Create |
| Upgrade Infisical                 | `docker compose pull && docker compose up -d` |

---

## 7. Production checklist

When promoting to a real environment:

1. Replace `SITE_URL=http://localhost:8080` with the public HTTPS URL.
2. Front Infisical with TLS (Caddy / nginx / Cloudflare Tunnel).
3. Move Postgres + Redis to managed instances (or at least to volumes
   with off-host backups).
4. Set `TELEMETRY_ENABLED=false` only if your security policy requires it.
5. Replace the `dev` machine identity with separate ones for `staging`
   and `prod`, each scoped to a single environment.
6. Rotate `ENCRYPTION_KEY` and `AUTH_SECRET` and re-encrypt the database
   following the Infisical upgrade guide before exposing the instance.

---

## 8. Troubleshooting

| Symptom                                              | Fix |
|------------------------------------------------------|-----|
| `401 Unauthorized` from Infisical at startup         | `BRUSCA__INFISICAL__CLIENTID` / `CLIENTSECRET` env vars missing or wrong scope. Re-do section 4. |
| `404 Project not found`                              | `Brusca:Infisical:ProjectId` does not match the project slug. |
| Secrets are cached too long                          | Lower `Brusca:Infisical:RefreshInterval`, or call `ISecretProvider.RefreshAsync` from a custom admin endpoint. |
| Containers crash on first start                      | `ENCRYPTION_KEY` must be **exactly 32 characters**. Regenerate `.env`. |
| Brusca.Api builds but DB calls fail                  | Check that `DatabaseConnectionString` actually exists in Infisical `dev` — falling back to `appsettings.json` only works if `Brusca:Infisical:Enabled` is `false`. |
