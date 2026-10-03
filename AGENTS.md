# AGENTS.md

## Cursor Cloud specific instructions

### Project overview

EList 3.0.1 is a **C# / ASP.NET Core 6.0** backend REST API for an event-management and social platform. It exposes a Swagger-documented API with endpoints for accounts, authorization, events, participations, subscriptions, media, wallets (tariff balance only), and ticket orders (feature-flagged stub).

**Product / domain docs (manual, not auto-updated on commit):** see [`docs/SERVICE.md`](docs/SERVICE.md) and [`docs/production-readiness-checklist.md`](docs/production-readiness-checklist.md). Legal drafts live in [`Agreements/`](Agreements/); runtime texts are in the DB.

### Tech stack

- **.NET 6 SDK** — target framework `net6.0`. On Ubuntu 24.04 install `dotnet-sdk-6.0` from the `ppa:dotnet/backports` archive (`sudo add-apt-repository -y ppa:dotnet/backports`).
- **PostgreSQL 16** with **PostGIS** (`postgresql-16-postgis-3`) and **uuid-ossp** (`postgresql-contrib`)
- **linq2db** ORM, **Npgsql** driver, **FluentMigrator** for schema
- **NuGet** for package management (implicit via `dotnet restore`)
- No JavaScript/Node.js components

### Sibling repository dependency

`EList.sln` references five projects at `../EList.Common/` (sibling of this repo). On Linux that path is case-sensitive and must be a real directory. A symlink breaks MSBuild when it resolves paths inside Common. Clone the public `develop` branch:

```bash
sudo git clone --depth 1 --branch develop https://github.com/crash240192/elist.common.git "$(dirname "$(pwd)")/EList.Common"
sudo chown -R "$(id -u):$(id -g)" "$(dirname "$(pwd)")/EList.Common"
```

A checkout named `elist.common` does not satisfy the solution path.

### Building and running

From the repo root (Cloud Agents use `/workspace`):

```bash
dotnet restore EList.sln
dotnet build EList.sln --no-restore
cd EList.Api
ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS="http://127.0.0.1:5131" dotnet run --no-launch-profile
```

`--no-launch-profile` keeps `launchSettings.json` from adding the HTTPS URL. Health: `http://127.0.0.1:5131/eList/health`. Swagger: `http://127.0.0.1:5131/eList/swagger/index.html`.

### Database

Use a local PostgreSQL 16 + PostGIS database. Do not point agents at the remote host stored in `appsettings.json`.

`ConfigurationManager.Initialize` takes the ASP.NET Core host configuration, so environment variables override JSON. Set:

```bash
export connectionStrings__elist_main_db__connectionString="Host=127.0.0.1;Port=5432;Username=elist;Password=elist;Database=elist"
```

Create role `elist` (login, superuser, password `elist`) and database `elist`, then `CREATE EXTENSION postgis` and `CREATE EXTENSION "uuid-ossp"`.

Apply schema as the `postgres` user, in this order:

1. `EList.Database/Migrations/InitialDatabase.sql` (idempotent fresh-install baseline)
2. The later registered SQL files, which the baseline does not fully include: `M202609031136_soft_delete_catalogs.sql`, `M202609091542_message_votes.sql`, `M202609142252_cover_focus.sql`, `M202609152340_message_files_system_album.sql`, `M202609211200_ticket_refund_pending.sql`, `M202609211530_wallet_deposits.sql`, `M202609211700_wallet_next_charge.sql`, `M202609221000_wallet_deposit_balance_after.sql`

Skip `M202608250914_develop_incremental.sql` on a database created from the current baseline. It upgrades older databases and is not idempotent here.

Registration requires rows in `documents` for types `consent` and `agreement`. Insert local placeholders when those types are missing. Without them, `POST /api/accounts/create` returns `AgreementDocumentNotFound`.

Background workers call external services. For local runs, set `debtCollector__active`, `organizationVerification__active`, `retentionPurge__active`, and `orphanFileGc__active` to `false`.

### Authentication for API calls

All endpoints require an `Authorization-jwt` header (any non-empty string; it gets hashed for client identification). Account creation (`POST /api/accounts/create`) and login (`POST /api/authorization`) only need this header. Other endpoints also require an `Authorization` header containing a valid token UUID.

### Key gotchas

- Host configuration (JSON, environment variables, user secrets, command line) is what `ConfigurationManager` reads after `Initialize`. Override the database with `connectionStrings__elist_main_db__connectionString` instead of editing `appsettings.json`.
- `UseHttpsRedirection()` is enabled. With `ASPNETCORE_URLS` set to HTTP only and `--no-launch-profile`, requests to `http://127.0.0.1:5131` are not redirected.
- No test projects exist in this codebase; there are no automated tests to run.
- Build produces ~318 XML doc warnings (missing XML comments). These are expected.
- The app path base is `/eList` — all API routes are prefixed with `/eList/api/...`.
