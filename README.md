# CMS API

A .NET 10 API service that ingests CMS events via a webhook and exposes entity data via a REST API.

## Tech Stack

- .NET 8/9/10
- EF Core
- SQL Server (MSSQL)
- Minimal API / Controllers
- Swagger UI for testing
- Basic Authentication for webhook endpoint

## Project Structure

CMSApi
├── Controllers
│ ├── CmsEntityController.cs – handles webhook events (publish, update, unPublish, delete)
│ └── EntityController.cs – exposes entity data via REST API
│
├── Data
│ ├── Repository
│ │ ├── ICmsEntityRepository.cs – repository interface for entities
│ │ ├── ICmsEntityVersionRepository.cs – repository interface for entity versions
│ │ ├── CmsEntityRepository.cs – repository implementation
│ │ └── CmsEntityVersionRepository.cs – repository implementation
│ │
│ └── ApplicationDbContext.cs – EF Core DbContext and database configuration
│
├── Domain
│ ├── CmsEntity.cs – main entity model
│ └── CmsEntityVersion.cs – versioned data for entities
│
├── Dtos
│ └── CmsEntityDto.cs – incoming CMS webhook event schema
│
├── Services
│ ├── ICmsEntityService.cs – service interface for CMS event processing
│ ├── IEntityService.cs – service interface for entity operations
│ ├── CmsEntityService.cs – event processing implementation
│ └── EntityService.cs – entity-related business logic
│
├── Authentication
│ ├── BasicAuthOptions.cs – configuration options for Basic Authentication
│ └── BasicAuthenticationService.cs – handles Basic Authentication logic
│
├── Migrations – EF Core database migrations
│
├── Properties
│
├── Tests
│ └── CmsApi.Tests – unit tests for CMSApi
│
├── appsettings.json – application configuration
├── Program.cs – application entry point and dependency injection setup
├── CMSApi.http – REST API & webhook test cases
├── .gitignore
└── README.md

---

## Getting Started

### Prerequisites

- .NET 9 or 10 SDK  
- Git  
- SQL Server (MSSQL)  

### Run locally

```bash
dotnet restore
dotnet build
dotnet run
```

## Git workflow

- main → stable, production-ready

- dev → development

- staging → pre-release testing

### Webhook API

POST /cms/events

Handles CMS events: publish, update, delete, unPublish

JSON example:

```
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
```

- payload can be null for delete events

- Event versioning is managed via CmsEntity and CmsEntityVersion models

## REST API

- EntitiesController – lists entities for consumers

- Admin users can see all entities, including disabled ones

- Regular users cannot modify data via the REST API

- Admins can override entity status (disable) locally, without affecting the CMS

## Authentication

- The /cms/events webhook endpoint is protected with Basic Authentication.
- Implemented via the Authentication/BasicAuthenticationHandler.cs and BasicAuthOptions.cs.
- Credentials are stored securely in appsettings.json:
```
"BasicAuth": {
  "BasicUsername": "basic_user",
  "BasicPassword": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "AdminUsername": "admin_user",
  "AdminPassword": "e02b2c3d-479f-47ac-10b5-8cc4372a5670"
}
```
- CMS user → BasicUsername / BasicPassword
- Admin user → AdminUsername / AdminPassword


## Flow

1. CMS webhook event arrives at /cms/events

2. CmsEventProcessor processes events:

- publish → saves a new version

- update → updates existing entity

- unPublish → disables entity but keeps it in database

- delete → permanently deletes the entity

3. REST API exposes entities to users and admins

## Testing

- The .http file allows testing publish, update, delete events easily

- Swagger UI is available for quick manual testing


## Data Privacy

- All CMS data should be treated as confidential / restricted.

- Do not expose any entity publicly.

## Event Handling & Versioning

- The webhook can send batch events (multiple events in a single request).

- Event types: publish, update, delete, unPublish.

- Versioning rules:

    - Each update creates a new version (X → X+1).
    - If an entity is unpublished and there was no previously published version, latest version may be unavailable — handle this edge case

## Application Layer Rules

- Validate and sanitize all incoming data.

- Delete events → hard delete in the database.

- Unpublish events → keep the entity in the database but mark it disabled.

## Performance

- Using asynchronous mechanisms for event processing improves throughput and latency.

- EF Core context is configured with read-only / writer separation to optimize read queries.


## Observability & Logging

- Log all processed events, including failures.

- Include timestamps and event metadata in logs for debugging and auditing.
