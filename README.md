# CMS API

A .NET 10 API service that ingests CMS events via a webhook and exposes entity data via a REST API.

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
│ ├── CmsEventsController.cs
│ └── EntitiesController.cs
├── Domain
│ ├── CmsEntity.cs
│ └── CmsEntityVersion.cs
├── Infrastructure
│ └── ApplicationDbContext.cs
├── Services
│ ├── ICmsEventProcessor.cs
│ └── CmsEventProcessor.cs
├── Dtos
│ └── CmsEventDto.cs
├── Program.cs
├── CMSApi.csproj
├── CMSApi.http
└── README.md


## Getting started

## Prerequisites

- .NET 8 or 9 SDK
- Git
- SQL Server or SQLite

### Run locally

```bash
dotnet restore
dotnet build
dotnet run
```

## Git workflow

main → stable, production-ready

dev → development

staging → pre-release testing

### Webhook API

POST /cms/events

Handles CMS events: publish, update, delete, unPublish

JSON example:

[
  {
    "type": "publish",
    "id": "entity1",
    "payload": {
      "name": "Test Entity",
      "description": "This is a test"
    },
    "version": 1,
    "timestamp": "2024-01-01T00:00:00Z"
  }
]

payload can be null for delete events

Event versioning is managed via CmsEntity and CmsEntityVersion models

## REST API

EntitiesController – lists entities for consumers

Admin users can see all entities, including disabled ones

Regular users cannot modify data via the REST API

Admins can override entity status (disable) locally, without affecting the CMS

## Flow

CMS webhook event arrives at /cms/events

CmsEventProcessor processes events:

publish → saves a new version

update → updates existing entity

unPublish → disables entity but keeps it in database

delete → permanently deletes the entity

REST API exposes entities to users and admins

## Testing

The .http file allows testing publish, update, delete events easily

Swagger UI is available for quick manual testing

## CMS Webhook Authentication

The /cms/events endpoint is protected with Basic Authentication.

CMS uses a dedicated username and password (different from user REST API credentials).

Example credentials format (do not hardcode in production):

Username: random string (10-20 characters)
Password: random GUID

## Data Privacy

All CMS data should be treated as confidential / restricted.

Do not expose any entity publicly.

## Event Handling & Versioning

The webhook can send batch events (multiple events in a single request).

Event types: publish, update, delete, unPublish.

Versioning rules:

Each update creates a new version (X → X+1).

If an entity is unpublished and there was no previously published version, the latest version may be unavailable — handle this edge case.

Event processing is asynchronous, which allows the system to:

Efficiently handle large batches of incoming events without blocking the webhook response.

Scale better under high load, processing multiple events concurrently.

Keep the API responsive for clients while the database operations complete in the background.

## Application Layer Rules

Validate and sanitize all incoming data.

Delete events → hard delete in the database.

Unpublish events → keep the entity in the database but mark it disabled.

## Performance

Using asynchronous mechanisms for event processing improves throughput and latency.

EF Core context is configured with read-only / writer separation to optimize read queries.


## Observability & Logging

Log all processed events, including failures.

Include timestamps and event metadata in logs for debugging and auditing.
