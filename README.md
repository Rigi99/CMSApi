# CMS API

.NET 10 Web API that ingests CMS webhook events, stores versioned entities with EF Core, and exposes a restricted REST API with Basic Authentication + role policies.

## Key Features

- Batch webhook ingestion on `/cms/events`
- Event types: `publish`, `update`, `unpublish`, `delete`
- Version history in `CmsEntityVersions`
- Hard delete on `delete`, local disable on `unpublish` and admin override
- Role-separated API access (`CmsIngest`, `ApiUser`, `Admin`)

## Tech Stack

- .NET 10
- EF Core 10
- SQL Server + SQLite providers
- ASP.NET Core Controllers
- xUnit + Moq tests

## Authentication & Authorization

Basic auth credentials are read from `BasicAuth` config and mapped to roles:

- `BasicUsername` / `BasicPassword` -> `CmsIngest`
- `ApiUsername` / `ApiPassword` -> `ApiUser`
- `AdminUsername` / `AdminPassword` -> `Admin`

Policies:

- `CmsIngestPolicy`: only `CmsIngest` role (webhook ingestion)
- `ApiReadPolicy`: `ApiUser` or `Admin` (entity read endpoints)
- `AdminPolicy`: only `Admin` (admin-only endpoints)

## Endpoints

- `POST /cms/events` (CmsIngest only)
- `GET /api/entities` (ApiUser/Admin)
- `GET /api/entities/admin` (Admin only)
- `PATCH /api/entities/{id}/disable` (Admin only)

## Database Configuration

The app supports provider switching with `DatabaseProvider`.

- `SqlServer` (default in `appsettings.json` and `appsettings.Development.json`)
- `Sqlite` (optional, can be enabled manually for local cross-platform dev)

### Default config

`appsettings.json`:

- `DatabaseProvider`: `SqlServer`
- `ConnectionStrings:DefaultConnection`: LocalDB SQL Server connection

`appsettings.Development.json`:

- `DatabaseProvider`: `SqlServer`
- `ConnectionStrings:SqliteConnection`: `Data Source=cmsapi.dev.db`

## Run Locally

```bash
dotnet restore
dotnet build
dotnet run
```

By default, Development environment runs on SQLite and works on both Windows and macOS/Linux.
By default, Development environment runs on SQL Server. If you want to use SQLite locally, change `DatabaseProvider` in `appsettings.Development.json` to `Sqlite`.

On Windows, the default `DefaultConnection` uses LocalDB.

On macOS/Linux, use one of these options:

- switch `DatabaseProvider` to `Sqlite` for file-based local development
- keep `SqlServer` and point `DefaultConnection` to a SQL Server container

## SQL Server in Docker (Optional)

If you prefer SQL Server cross-platform, you can run it in Docker:

```bash
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=YourStrong!Passw0rd" -p 1433:1433 --name cms-sql -d mcr.microsoft.com/mssql/server:2022-latest
```

Then set in `appsettings.Development.json`:

- `DatabaseProvider`: `SqlServer`
- `ConnectionStrings:DefaultConnection`: `Server=localhost,1433;Database=CMSApiDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;`

## Testing

Run all tests:

```bash
dotnet test CMSApi.Tests/CMSApi.Tests.csproj --nologo
```

## Notes

- All CMS data is treated as restricted.
- Incoming events are validated before processing.
- Event processing is transactional with logging for both success and failure paths.
- Read/write separation is implemented with dedicated EF Core contexts (`ApplicationDbContext` for writes, `ReadOnlyApplicationDbContext` for read queries).
