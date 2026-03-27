# OpenJobEngine Roadmap

This document defines the product and engineering roadmap that should guide future releases of OpenJobEngine.

It is based on the current system state after `v0.4.0`.

## Current system

OpenJobEngine already includes a functional backend-first baseline with these capabilities:

- Multi-source job collection through `Computrabajo`, `Adzuna`, `Greenhouse` and `Lever`
- Canonical normalization of job offers
- Enrichment of work mode, seniority, salary, location, skills and language signals
- Deduplication through canonical keys
- Persistence with `SQLite` by default and `PostgreSQL` as optional backend
- Job lifecycle tracking with source observations and history entries
- Candidate profile CRUD
- Candidate skills, languages, salary and location preferences
- Saved searches and profile alerts as persisted entities
- Real alert dispatch pipeline with persisted alert deliveries and webhook publishing
- Resume import from PDF with stronger heuristic extraction, warnings and field confidences for Spanish and English CVs
- Deterministic explainable matching with JSON-configured rules, hard requirements and preference-aware scoring
- Metrics endpoints for overview, provider quality, provider operations, matching and alerts
- Separate worker process for scheduled collection
- Git Flow release process and centralized semantic versioning
- Exportable release notes workflow and a lightweight API compatibility policy

## Current limitations

The project is already useful for demos and internal validation, but it is not yet production-grade. Current gaps:

- Alert delivery is functional, but downstream automation and long-running operational hardening still need to mature
- Resume parsing is materially better, but still heuristic only
- Matching is explainable and richer than the demo baseline, but still limited in alerting and downstream automation
- Providers are disabled by default and require manual configuration
- Provider coverage is still small
- No multi-tenant model
- API compatibility discipline has started, but there is not yet a full contract-diff workflow

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

#### `v0.2.1-beta.2`

Status:

- released

Goal:

- improve matching usefulness for real users

Delivered:

- hard requirements, tolerances and stronger penalties in JSON-configured matching rules
- preferred timezone scoring and excluded work mode handling
- include/exclude company keyword preference matching
- `GET /api/profiles/{profileId}/matches/new-high-priority`
- richer match execution statistics and high-priority counters
- integration tests for high-priority matching flows and hard-failure explanations

### Next releases

#### `v0.3.0-beta.3`

Status:

- released

Goal:

- make the backend more marketable for integrators

Delivered:

- real alert dispatch pipeline
- webhook delivery for new relevant matches
- provider and matching operational dashboards through API
- improved API/docs coverage for integrators
- `Lever` as the next structured provider

#### `v0.4.0`

Status:

- released

Goal:

- turn OpenJobEngine into a candidate matching backend ready for broader beta adoption

Scope:

- improve resume parsing quality for Spanish and English CVs
- improve salary normalization across LATAM markets
- improve provider quality scoring and stale-job deactivation behavior
- add exportable release notes and stronger release governance
- begin API compatibility discipline

Delivered:

- stronger Spanish and English CV parsing with section-aware extraction, richer confidences and safer onboarding previews
- LATAM salary normalization for `k`, `mil`, `millones` and local currency inference from structured locations
- provider freshness windows with stale observation deactivation even when a provider run fails
- richer provider quality metrics with trusted salary ratios, low-quality ratios and average freshness hours
- release notes export workflow, compatibility guidance and API compatibility tests

Release criteria:

- onboarding quality is materially better
- matching and enrichment are more trustworthy across common LATAM scenarios
- release process is repeatable and auditable

### Next release target

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

The next release target after `v0.4.0` should be:

- `v1.0.0`

Reason:

- the backend already covers aggregation, matching, alerting and release governance, so the next step is to stabilize contracts, deployment guidance and production expectations for the first stable major.
