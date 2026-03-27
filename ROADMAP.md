# OpenJobEngine Roadmap

This document defines the product and engineering roadmap that should guide future releases of OpenJobEngine.

It is based on the current system state after `v0.1.0-demo.1`.

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

- No authentication or API key protection
- No health checks or readiness endpoints
- No centralized `ProblemDetails` error handling
- No rate limiting for expensive endpoints
- Database schema still uses `EnsureCreated`, not EF migrations
- Alerts are persisted, but there is no real alert dispatch pipeline yet
- Worker scheduling is basic: fixed interval, no retries, no provider-level concurrency guards
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

### Next releases

#### `v0.1.1-demo.2`

Goal:

- make the demo cleaner, more consistent and easier to run end to end

Scope:

- add centralized API exception handling
- return consistent `ProblemDetails` payloads for common failures
- improve Swagger descriptions for the current public endpoints
- tighten the manual demo flow and smoke documentation
- expose version consistently in Swagger and API responses
- tighten local provider configuration guidance for demos

Explicitly out of scope:

- EF migrations
- API keys
- rate limiting
- worker retries and backoff
- new providers
- alert dispatch implementation

Release criteria:

- API starts cleanly with SQLite
- demo flow can be executed without undocumented steps
- failures return consistent error payloads
- the public demo surface looks coherent in Swagger
- the release remains small and does not introduce new platform subsystems

#### `v0.2.0-beta.1`

Goal:

- establish the first operational beta baseline without expanding product scope

Scope:

- replace `EnsureCreated` with EF migrations
- add optional API key authentication
- add readiness and liveness checks
- add basic rate limiting for collection, resume import and webhook test
- prevent overlapping worker executions for the same source
- improve worker reliability with retries and backoff

Explicitly out of scope:

- richer matching business rules
- alert delivery pipeline
- new providers
- multi-tenant model
- advanced analytics endpoints
- major resume parsing redesign

Release criteria:

- schema is reproducible through migrations
- protected mode can be enabled with API keys
- liveness and readiness endpoints reflect real system state
- rate limiting protects the most expensive endpoints
- worker is safer under provider failures and overlapping schedules
- beta branch is suitable for external validation of the backend contract

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

The next release target after `v0.1.0-demo.1` should be:

- `v0.1.1-demo.2`

Reason:

- it tightens the demo and operating surface without prematurely jumping into a large beta scope.
