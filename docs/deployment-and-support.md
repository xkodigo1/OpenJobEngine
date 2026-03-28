# Deployment And Support

This document defines the operational baseline for OpenJobEngine consumers.

## What the system runs on

OpenJobEngine supports two persistence modes:

- `SQLite` for local development, demos and fast onboarding
- `PostgreSQL` for production and shared environments

SQLite is the default because it keeps the open source entry path simple. PostgreSQL is the production target because it is the safest option for concurrent workloads, longer retention and multi-process usage.

## How to deploy

The repository has two runtime entrypoints:

- `OpenJobEngine.Api` exposes the HTTP API
- `OpenJobEngine.Worker` runs scheduled collection and alert dispatch

Recommended deployment modes:

- local demo: run the API only with SQLite
- development parity: run API and Worker locally with SQLite
- production: run API and Worker as separate processes against PostgreSQL

The worker is optional for manual demos, but required if you want continuous collection and alert dispatch without triggering runs by hand.

## Migration policy

The current codebase uses EF Core migrations as the schema evolution mechanism.

Operational rule:

- local environments may apply migrations on startup when `Persistence__ApplyMigrationsOnStartup=true`
- production deployments should treat schema changes as part of the release process
- do not rely on ad-hoc database creation

Practical release steps:

1. deploy the new binaries
2. apply the matching EF migration set
3. verify `/health/ready`
4. enable providers only after the database is ready

## Stable core API contract

The stable contract is the public `/api` surface that consumers should integrate with first:

- `/api/jobs`
- `/api/profiles`
- `/api/collections`
- `/api/matching`
- `/api/metrics`
- `/api/alerts`

Contract rules:

- keep `application/problem+json` as the error envelope
- keep the `X-OpenJobEngine-Version` header aligned with the tagged release
- prefer additive changes over renames or removals
- release any breaking change as a deliberate version decision

## Support expectations

OpenJobEngine is an open source backend, not a managed SaaS.

Support expectations for consumers are:

- the latest stable tag is the primary supported baseline
- beta and demo tags are provided for evaluation, not long-term compatibility
- compatibility changes should be documented in `CHANGELOG.md` and the exported release notes
- `docs/api-compatibility.md` is the short checklist before publishing a release

## Validation checklist

Before you promote a deployment:

1. confirm the database mode is correct for the environment
2. confirm migrations are applied
3. confirm the API and Worker are running with the intended configuration
4. confirm `/health/live` and `/health/ready`
5. confirm the release tag, version header and release notes match
