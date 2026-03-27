# Changelog

All notable changes to this project should be documented in this file.

The format is based on Keep a Changelog and the versioning strategy documented in `docs/versioning.md`.

## [Unreleased]

No changes yet.

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
