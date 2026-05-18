# AGENTS.md

## Cursor Cloud specific instructions

### Project overview

EList 3.0.1 is a **C# / ASP.NET Core 6.0** backend REST API for an event-management and social platform. It exposes a Swagger-documented API with endpoints for accounts, authorization, events, participations, subscriptions, media, and wallets.

### Tech stack

- **.NET 6 SDK** — target framework `net6.0`
- **PostgreSQL 16** with **PostGIS** (geospatial queries) and **uuid-ossp** extensions
- **linq2db** ORM, **Npgsql** driver, **FluentMigrator** for schema
- **NuGet** for package management (implicit via `dotnet restore`)
- No JavaScript/Node.js components

### Sibling repository dependency

The solution references 5 projects from the **EList.Common** sibling repo at `../EList.Common/` (relative to the workspace root, i.e., `/EList.Common`). This repo must be cloned there before building:

```
git clone https://github.com/crash240192/EList.Common.git /EList.Common
sudo chown -R $(whoami):$(whoami) /EList.Common
```

### Building and running

```bash
# Restore and build
dotnet restore /workspace/EList.sln
dotnet build /workspace/EList.sln --no-restore

# Run the API (from the EList.Api directory)
cd /workspace/EList.Api
ASPNETCORE_URLS="http://localhost:5131" dotnet run --no-build
```

The app starts on **http://localhost:5131** and **https://localhost:7020**.  
Swagger UI is at `http://localhost:5131/eList/swagger/index.html`.

### Database

The `appsettings.json` connection string points to a remote PostgreSQL server at `92.118.113.6:5432`. A local PostgreSQL+PostGIS instance is also available and can be used by modifying the connection string host to `127.0.0.1`.

The initial schema migration SQL is at `EList.Database/Migrations/InitialDatabase.sql`. Note: the raw SQL has table-ordering issues (references to `wallets` and `event_categories` before they are created). When applying locally, create `wallets` before `accounts`, and use the table name `event_categories` (the SQL creates `event_categories2` but `event_types` references `event_categories`).

### Authentication for API calls

All endpoints require an `Authorization-jwt` header (any non-empty string; it gets hashed for client identification). Account creation (`POST /api/accounts/create`) and login (`POST /api/authorization`) only need this header. Other endpoints also require an `Authorization` header containing a valid token UUID.

### Key gotchas

- The custom `EList.Common.Configuration.ConfigurationManager` reads **only** from `appsettings.json` (not environment variables or `appsettings.Development.json`). To change the DB connection string for local development, you must edit `appsettings.json` directly.
- `UseHttpsRedirection()` is enabled — HTTP requests redirect to HTTPS. Use `curl -k` or target the HTTPS port directly.
- No test projects exist in this codebase; there are no automated tests to run.
- Build produces ~318 XML doc warnings (missing XML comments). These are expected.
- The app path base is `/eList` — all API routes are prefixed with `/eList/api/...`.
