# CMS API

A .NET 9 (vagy 8) API service that ingests CMS events via a webhook and exposes entity data via a REST API.

## Tech stack

- .NET 8/9
- EF Core
- SQL Server / SQLite
- Minimal API / Controllers
- Swagger UI for testing
- Basic Authentication for webhook endpoint

## Project structure
CMSApi
├── Controllers
│ ├── CmsEventsController
│ └── EntitiesController
├── Domain
│ ├── CmsEntity.cs
│ └── CmsEntityVersion.cs
├── Infrastructure
│ └── ApplicationDbContext.cs
├── Services
│ └── CmsEventProcessor.cs
├── Dtos
│ └── CmsEventDto.cs
├── Program.cs
└── CMSApi.csproj


## Getting started

### Prerequisites

- .NET 8 or 9 SDK
- Git
- SQL Server or SQLite

### Run locally

```bash
dotnet restore
dotnet build
dotnet run

Git workflow

main → stable, production-ready

dev → development

staging → pre-release testing

