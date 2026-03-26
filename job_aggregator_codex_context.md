# JobAggregator – Codex Development Context

## 1. Project Overview

**Project Name:** JobAggregator  
**Type:** Open Source Backend System  
**Goal:** Build a modular, extensible multi-source job aggregation engine.

The system collects job offers from multiple providers (scraping and APIs), normalizes them into a unified format, removes duplicates, stores them in a database, and exposes them through a REST API.

---

## 2. MVP Scope

### Must include:
- Multi-source job collection (at least 2 providers)
- Data normalization
- Deduplication
- PostgreSQL database
- REST API with filters
- Background or manual job collection
- Clean architecture

### Must NOT include:
- Authentication system
- Complex frontend
- Proxies / anti-bot bypass logic
- Microservices
- ML or recommendation systems

---

## 3. Tech Stack

- Language: C#
- Framework: .NET 8/9
- API: ASP.NET Core Web API
- Database: PostgreSQL
- ORM: EF Core (preferred) or Dapper
- Scraping: Playwright + HttpClient

---

## 4. Architecture

Use Clean Architecture / Hexagonal Architecture.

### Layers:

- Domain
- Application
- Infrastructure
- API
- Worker (Background jobs)

### Solution structure:

```
/src
  /JobAggregator.Domain
  /JobAggregator.Application
  /JobAggregator.Infrastructure
  /JobAggregator.Api
  /JobAggregator.Worker
/tests
/docs
```

---

## 5. Core Domain Model

### Entity: JobOffer

Fields:
- Id (Guid)
- Title
- CompanyName
- Description
- LocationText
- EmploymentType
- SeniorityLevel
- SalaryText
- SalaryMin
- SalaryMax
- SalaryCurrency
- IsRemote
- Url
- SourceName
- SourceJobId
- PublishedAtUtc
- CollectedAtUtc
- DeduplicationKey
- IsActive

---

## 6. Raw Model (Before Normalization)

### RawJobOffer

Used as intermediate structure returned by providers.

Fields:
- SourceName
- SourceJobId
- Title
- CompanyName
- Description
- LocationText
- SalaryText
- Url
- PublishedAtUtc
- Metadata (Dictionary<string,string>)

---

## 7. Providers (Critical Component)

### Interface

```csharp
public interface IJobProvider
{
    string SourceName { get; }
    Task<IReadOnlyCollection<RawJobOffer>> CollectAsync(CancellationToken cancellationToken);
}
```

### Initial Providers:
- ComputrabajoProvider (HTML scraping)
- IndeedProvider (basic scraping)
- ApiProvider (external API like Adzuna/Jooble)

### Rules:
- Providers ONLY fetch and parse data
- Providers MUST NOT access database
- Providers MUST return RawJobOffer

---

## 8. Processing Pipeline

Flow:

```
Provider → RawJobOffer → Normalization → Deduplication → Persistence
```

---

## 9. Normalization

### Interface

```csharp
public interface INormalizationService
{
    JobOffer Normalize(RawJobOffer raw);
}
```

### Responsibilities:
- Normalize titles
- Detect remote jobs
- Parse salary text into structured values
- Standardize employment types
- Clean text

---

## 10. Deduplication

### Interface

```csharp
public interface IDeduplicationService
{
    string BuildKey(RawJobOffer raw);
}
```

### Strategy (MVP):

Key based on:
- Title
- Company
- Location

Example:
```
software-engineer|acme|bogota
```

---

## 11. Database Design (PostgreSQL)

### Tables

#### job_offers
Stores normalized job offers

#### job_sources
Stores available providers

#### scrape_executions
Logs scraping runs

#### job_offer_tags (optional)
Stores extracted technologies

### Key Requirements:
- Unique index on (source_name, source_job_id)
- Index on deduplication_key
- Index on title and company

---

## 12. Repository Interfaces

```csharp
public interface IJobRepository
{
    Task AddAsync(JobOffer job);
    Task UpdateAsync(JobOffer job);
    Task<JobOffer?> GetByDedupKeyAsync(string key);
    Task<IEnumerable<JobOffer>> SearchAsync(...);
}
```

```csharp
public interface IScrapeExecutionRepository
{
    Task CreateAsync(...);
    Task UpdateAsync(...);
}
```

---

## 13. Application Services

### JobCollectionService

Responsibilities:
- Execute all providers
- Normalize results
- Deduplicate
- Persist data
- Track execution metrics

---

## 14. API Endpoints

### Jobs
- GET /api/jobs
- GET /api/jobs/{id}
- GET /api/jobs/search

### Filters
- query
- location
- remote
- salaryMin
- salaryMax
- source

### Collection
- POST /api/collections/run
- POST /api/collections/run/{source}
- GET /api/collections/executions

---

## 15. Worker

Responsibilities:
- Trigger job collection periodically
- Or expose manual execution

Initial implementation:
- Manual trigger via API

Future:
- BackgroundService
- Scheduler (Hangfire/Quartz)

---

## 16. Rules & Constraints

### Architecture Rules:
- No business logic in controllers
- No DB access in providers
- No coupling between layers
- Domain must be pure
- Use Dependency Injection

### Scraping Rules:
- No aggressive scraping
- No CAPTCHA bypass
- No login automation
- Respect delays and rate limits

---

## 17. Testing

### Unit Tests:
- Normalization
- Deduplication
- Provider parsing

### Integration Tests:
- Repository
- API endpoints

---

## 18. Open Source Requirements

- README.md
- CONTRIBUTING.md
- .env.example
- Clear instructions to add providers

### Providers must be:
- Plug-and-play
- Optional
- Disabled by default if needed

---

## 19. Development Order

1. Solution structure
2. Domain entities
3. Application interfaces
4. Database + migrations
5. Repository implementations
6. Normalization service
7. Deduplication service
8. First provider
9. Second provider
10. JobCollectionService
11. API endpoints
12. Tests
13. Documentation

---

## 20. Final Goal

This project should NOT be just a scraper.

It must be:

**A modular, extensible job aggregation engine with clean architecture, multiple providers, and a scalable data pipeline.**

---

## 21. Instructions for Codex

- Generate production-ready code
- Follow clean architecture strictly
- Prioritize extensibility over shortcuts
- Avoid hardcoding providers
- Keep components reusable
- Document key parts of the system

Start by generating the full solution structure, then implement layer by layer following the development order.

