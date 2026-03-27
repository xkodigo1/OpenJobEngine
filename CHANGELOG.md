# Changelog

All notable changes to this project should be documented in this file.

The format is based on Keep a Changelog and the versioning strategy documented in `docs/versioning.md`.

## [Unreleased]

- Added exportable release notes tooling for tag and GitHub Release bodies.
- Added API compatibility guidance for the public `/api` surface.

## [0.3.0-beta.3] - 2026-03-27

### Added

- Real alert dispatch pipeline with manual dispatch endpoint, alert delivery persistence, webhook publishing and delivery metrics.
- `Lever` as an additional structured provider, including paging support and provider tests.
- Operational dashboards through `GET /api/metrics/alerts`, `GET /api/metrics/matching` and `GET /api/metrics/providers/operations`.
- Integration tests covering alert dispatch, alert metrics and Lever provider behavior.

### Changed

- Worker scheduling can now dispatch alerts after collection cycles with retry-aware source execution.
- API and documentation surface now describe alerts, operational dashboards and the expanded provider set.
- EF migration baseline now includes `alert_deliveries` and matching/alert snapshots aligned with the current model.

### Fixed

- SQLite compatibility for alert, matching and scrape execution metrics that use `DateTimeOffset` windows.
- Aggregate persistence for candidate alerts and alert deliveries under EF Core tracked graphs.

## [0.2.1-beta.2] - 2026-03-27

### Added

- Preference-aware matching for target timezones, excluded work modes, and company keyword include/exclude signals.
- `GET /api/profiles/{profileId}/matches/new-high-priority` for discovering newly relevant opportunities.
- Richer match execution statistics including strong, partial and hard-failure counts plus new high-priority totals.
- Integration tests covering high-priority matching flows and hard-failure explanations.

### Changed

- Matching rules now distinguish hard requirements, configurable tolerances and stronger business penalties.
- Match results now expose `strongMatches`, `partialMatches` and `hardFailures` alongside the existing explainability fields.

### Fixed

- SQLite compatibility for repository ordering paths used by matching, history and recent execution queries.
- EF Core detail loading for jobs now uses split queries to avoid multi-collection include warnings under integration tests.

## [0.2.0-beta.1] - 2026-03-27

### Added

- EF Core migrations baseline and design-time `DbContext` factory.
- `/health/live` and `/health/ready` endpoints with database, catalog and matching-rules checks.
- Optional API key protection through `X-Api-Key`.
- Rate limiting for collection runs, resume imports and webhook test calls.
- Worker retries, backoff and in-memory source execution guards.
- Integration tests for health, API key enforcement and rate limiting.

## [0.1.1-demo.2] - 2026-03-27

### Added

- Repository roadmap with staged releases from demo to stable.
- Integration tests covering Swagger version exposure and `application/problem+json` responses.

### Changed

- Centralized API exception handling with consistent `ProblemDetails` payloads.
- Swagger/OpenAPI descriptions for the current public controllers and endpoints.
- Demo smoke flow documentation with version header expectations.

### Fixed

- EF Core aggregate navigation mappings for `CandidateProfile` and `JobOffer` collections.
- Version header and `ProblemDetails` payloads now expose the clean release version without build metadata.

## [0.1.0-demo.1] - 2026-03-27

### Added

- Multi-source job collection with `Computrabajo`, `Adzuna` and `Greenhouse` providers.
- Normalization, enrichment, deduplication and job history tracking.
- Candidate profiles, resume PDF import and explainable deterministic matching.
- SQLite-first local startup and provider quality metrics.
- Release governance with Git Flow using `main`, `develop`, `feature/*`, `release/*` and `hotfix/*`.
- Centralized semantic versioning from `Directory.Build.props`.
