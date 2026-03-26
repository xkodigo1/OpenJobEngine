# Contributing

## Principles

- Respect the layered architecture.
- Keep business rules out of controllers and providers.
- Providers only collect and parse `RawJobOffer`.
- Keep changes small, focused and documented.
- Use Conventional Commits with gitmoji in the subject line.

## Git Flow

OpenJobEngine uses Git Flow as the default development model.

- Permanent branches:
  - `main`: production-ready branch
  - `develop`: integration branch for completed work
- Temporary branches:
  - `feature/*`
  - `release/*`
  - `hotfix/*`

Rules:

- Never develop directly on `main` or `develop`.
- Start all new work from `develop`.
- Rebase your feature branch on top of `origin/develop` before finishing it.
- Squash your feature commits before finishing the branch.
- Only release branches and hotfix branches may land in `main`.

Use Git Flow directly through `git flow` or equivalent branch operations.

Full repo-specific guidance lives in `docs/gitflow.md`.

## Verification before finishing a branch

The original Git Flow guide mentions `tsc` and `npm run build`. For this repository the equivalent verification is:

```bash
dotnet restore OpenJobEngine.sln
dotnet build OpenJobEngine.sln -c Release
```

Run these checks before finishing `feature/*`, `release/*` and `hotfix/*` branches.

## Architecture conventions

- `Domain` does not depend on other layers.
- `Application` defines contracts and use cases.
- `Infrastructure` implements adapters and persistence.
- `Api` and `Worker` only orchestrate through DI.
- Document every new provider in `docs/adding-providers.md`.
