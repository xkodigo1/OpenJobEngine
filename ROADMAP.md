# OpenJobEngine Roadmap

This document defines the product and engineering roadmap that should guide future releases of OpenJobEngine.

It is based on the current system state after `v0.2.0-beta.1`.

## Current system

OpenJobEngine already includes a functional backend-first baseline with these capabilities:

- Multi-source job collection through `Computrabajo`, `Adzuna` and `Greenhouse`
- Canonical normalization of job offers
- Enrichment of work mode, seniority, salary, location, skills and language signals
- Deduplication through canonical keys
- Persistence with `SQLite` by default and `PostgreSQL` as optional backend
- Job lifecycle tracking with source observations and history entries
- Candidate profile CRUD
- Candidate skills, languages, salary and location preferences
- Saved searches and profile alerts as persisted entities
- Resume import from PDF with heuristic extraction, warnings and field confidences
- Deterministic explainable matching with JSON-configured rules
- Metrics endpoints for overview and provider quality
- Separate worker process for scheduled collection
- Git Flow release process and centralized semantic versioning

## Current limitations

The project is already useful for demos and internal validation, but it is not yet production-grade. Current gaps:

- Alerts are persisted, but there is no real alert dispatch pipeline yet
- Resume parsing is heuristic only
- Matching is explainable, but still limited in business rules depth
- Providers are disabled by default and require manual configuration
- Provider coverage is still small
- No multi-tenant model
- No hard compatibility policy for the API yet

## Product direction

OpenJobEngine should evolve from "job aggregation backend" into a "candidate matching and job intelligence backend".

That means the roadmap should prioritize:

1. Better operational reliability
2. Better candidate-job matching quality
3. Better integrator experience
4. Better marketable backend features
5. More providers only after the platform is stable

## Release strategy

Release stages:

- `demo`: prove end-to-end value
- `beta`: stabilize contracts and operations
- `stable`: publish with compatibility expectations

Branching and versioning:

- Git Flow is mandatory
- work starts from `develop`
- features use `feature/*`
- releases use `release/*`
- urgent fixes use `hotfix/*`
- tags use `v<version>`

## Release roadmap

### Released

#### `v0.1.0-demo.1`

Status:

- released

Goal:

- establish the first end-to-end demo baseline

Delivered:

- aggregation
- enrichment
- deduplication
- history and source observations
- candidate profiles
- resume PDF import
- explainable matching
- metrics
- SQLite-first local startup
- Git Flow and release versioning baseline

#### `v0.1.1-demo.2`

Status:

- released

Goal:

- make the demo cleaner, more consistent and easier to run end to end

Delivered:

- centralized API exception handling with consistent `ProblemDetails`
- Swagger/OpenAPI polish for the current public surface
- version exposure in Swagger, headers and problem payloads
- repository roadmap with staged releases
- integration tests for API error contracts and release version exposure

#### `v0.2.0-beta.1`

Status:

- released

Goal:

- establish the first operational beta baseline without expanding product scope

Delivered:

- EF migrations baseline and design-time `DbContext` factory
- optional startup migrations and compatibility guard for legacy local databases
- liveness and readiness endpoints with DB/catalog/rules checks
- optional API key protection through `X-Api-Key`
- rate limiting for collection, resume import and webhook test endpoints
- worker retries, backoff and in-memory guards against overlapping source runs
- integration tests covering health, API key and rate limiting behavior

### Next releases

#### `v0.2.1-beta.2`

Goal:

- improve matching usefulness for real users

Scope:

- add hard requirements vs soft preferences in matching rules
- incorporate preferred timezones into matching
- incorporate excluded work modes into matching
- incorporate include/exclude company keyword preferences
- add "new high-priority matches" endpoint for candidate profiles
- persist richer match execution statistics

Release criteria:

- different profiles produce visibly different rankings for the same jobs
- strong mismatches are explained clearly
- saved searches and alerts can use richer matching thresholds

#### `v0.3.0-beta.3`

Goal:

- make the backend more marketable for integrators

Scope:

- implement real alert dispatch pipeline
- add webhook delivery for new relevant matches
- add provider and matching operational dashboards through API
- improve OpenAPI coverage and integration docs
- add one more structured provider after platform stabilization

Release criteria:

- alerts are not just stored; they can be delivered
- integrators can consume the API with less custom reverse engineering
- provider coverage expands without lowering quality

#### `v0.4.0`

Goal:

- turn OpenJobEngine into a candidate matching backend ready for broader beta adoption

Scope:

- improve resume parsing quality for Spanish and English CVs
- improve salary normalization across LATAM markets
- improve provider quality scoring and stale-job deactivation behavior
- add exportable release notes and stronger release governance
- begin API compatibility discipline

Release criteria:

- onboarding quality is materially better
- matching and enrichment are more trustworthy across common LATAM scenarios
- release process is repeatable and auditable

#### `v1.0.0`

Goal:

- publish the first stable version

Scope:

- stable API contract for core endpoints
- production-grade schema evolution policy
- hardened worker operations
- documented deployment guidance
- release and support expectations for open source consumers

Release criteria:

- no major open gaps in core backend reliability
- API contract is considered stable
- docs are strong enough for third-party adoption

## Backlog candidates

These items are valuable, but should not jump ahead of the roadmap above unless priorities change:

- multi-tenant support
- SDKs or generated client libraries
- additional ATS and structured providers
- advanced analytics and labor-market intelligence endpoints
- LLM-assisted resume or job understanding
- richer recruiter-facing automation

## Prioritization rules

When deciding what to build next, apply these rules:

1. Do not add more providers before the platform is operationally stronger.
2. Do not add AI features before deterministic matching and parsing are solid.
3. Do not add frontend scope into the backend roadmap unless it becomes necessary for product adoption.
4. Prefer features that increase data quality, matching trust or integrator usability.
5. Every release should have a narrow goal, clear criteria and a justified scope.

## Immediate next step

The next release target after `v0.2.0-beta.1` should be:

- `v0.2.1-beta.2`

Reason:

- it is the next step that adds product value now that the operational baseline is in place.
