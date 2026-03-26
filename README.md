# OpenJobEngine

OpenJobEngine es un backend open source para agregacion, enrichment e indexacion de ofertas de empleo con matching explicable para candidatos tech LATAM.

No es solo un agregador. El proyecto ya incluye:
- recoleccion multi-fuente
- normalizacion y deduplicacion
- enrichment estructurado
- historico de cambios por vacante
- perfiles persistidos de candidato
- parsing heuristico de CV en PDF
- ranking configurable desde JSON
- alertas, saved searches y metricas operativas

## Arquitectura

```text
src/
  OpenJobEngine.Domain
  OpenJobEngine.Application
  OpenJobEngine.Infrastructure
  OpenJobEngine.Api
  OpenJobEngine.Worker
docs/
```

## Stack

- .NET 8
- ASP.NET Core Web API
- SQLite por defecto, PostgreSQL opcional
- EF Core
- HttpClient + Playwright para providers
- PdfPig para extraccion de texto desde PDF

## Flujo principal

```text
Provider -> RawJobOffer -> Normalization -> Enrichment -> Deduplication -> Persistence
                                             -> History + Source Observations
CandidateProfile + Resume PDF -> Profile Extraction -> Matching -> Ranked results
```

## Inicio rapido

API local con SQLite:

```powershell
.\scripts\start-api.ps1
```

Worker local con SQLite:

```powershell
.\scripts\start-worker.ps1
```

Linux/macOS:

```bash
./scripts/start-api.sh
./scripts/start-worker.sh
```

## Git Flow

The repository uses Git Flow for day-to-day development:

- `main`: production-ready branch
- `develop`: integration branch
- `feature/*`, `release/*`, `hotfix/*`: temporary branches

Direct commands:

```bash
git flow init --preset=classic --main main --develop develop --tag v
git flow feature start nombre-de-feature
git flow feature finish nombre-de-feature
```

Repo-specific guidance: `docs/gitflow.md`

## Versioning and releases

OpenJobEngine now uses centralized semantic versioning from `Directory.Build.props`.

Current baseline:

- version: `0.1.0-demo.1`
- stage: `demo`
- tag format: `v<version>`

Release progression:

- demo: `0.1.0-demo.1`
- beta: `0.1.0-beta.1`
- stable: `0.1.0`

Detailed strategy: `docs/versioning.md`
Release notes history: `CHANGELOG.md`

Tambien puedes correr directamente:

```bash
dotnet run --project src/OpenJobEngine.Api
dotnet run --project src/OpenJobEngine.Worker
```

Por defecto:
- se crea `data/openjobengine.db`
- no necesitas PostgreSQL
- no necesitas seed para skills o idiomas

## Configuracion de persistencia

Default local:

```env
Persistence__Provider=Sqlite
ConnectionStrings__Sqlite=Data Source=data/openjobengine.db
```

Produccion con PostgreSQL:

```env
Persistence__Provider=Postgres
ConnectionStrings__Postgres=Host=localhost;Port=5432;Database=openjobengine;Username=postgres;Password=postgres
```

## Endpoints principales

Jobs:
- `GET /api/jobs`
- `GET /api/jobs/search`
- `GET /api/jobs/{id}`
- `GET /api/jobs/{id}/history`
- `GET /api/jobs/{id}/match?profileId=...`

Coleccion:
- `POST /api/collections/run`
- `POST /api/collections/run/{source}`
- `GET /api/collections/executions`

Perfiles:
- `POST /api/profiles`
- `GET /api/profiles/{profileId}`
- `PUT /api/profiles/{profileId}`
- `PATCH /api/profiles/{profileId}/preferences`
- `PATCH /api/profiles/{profileId}/skills`
- `PATCH /api/profiles/{profileId}/languages`
- `POST /api/profiles/{profileId}/resume`
- `GET /api/profiles/{profileId}/matches`
- `GET /api/profiles/{profileId}/saved-searches`
- `POST /api/profiles/{profileId}/saved-searches`
- `POST /api/profiles/{profileId}/alerts`

Matching y operacion:
- `POST /api/matching/search`
- `GET /api/matching/rules`
- `GET /api/metrics/overview`
- `GET /api/metrics/providers`
- `POST /api/webhooks/test`

Swagger:
- `/swagger`

## Catalogos y reglas versionadas

No requieren seed en DB:
- `src/OpenJobEngine.Infrastructure/Catalog/Data/skills.json`
- `src/OpenJobEngine.Infrastructure/Catalog/Data/languages.json`
- `src/OpenJobEngine.Infrastructure/Catalog/Data/locations.json`
- `src/OpenJobEngine.Infrastructure/Matching/Data/matching-rules.json`

La DB queda para datos dinamicos:
- vacantes
- observaciones por fuente
- historico de vacantes
- perfiles
- alerts
- saved searches
- match executions

## Providers incluidos

- `Computrabajo`
- `Adzuna`
- `Greenhouse`

## Validacion manual recomendada

Usa la coleccion HTTP versionada en:
- `docs/http/OpenJobEngine.http`

Flujo recomendado:
1. levantar API con SQLite
2. ejecutar una coleccion
3. crear perfil
4. subir CV
5. pedir matches
6. revisar historico y metricas

## Estado actual

- `dotnet build OpenJobEngine.sln -c Release`: OK
- sin autenticacion en el core
- parsing de CV heuristico pero con warnings y confidencias
- matching configurable por JSON y explicable
- SQLite listo para onboarding open source

## Documentacion adicional

- [Adding providers](/C:/Users/xkodi/OJE/docs/adding-providers.md)
- [Catalogs](/C:/Users/xkodi/OJE/docs/catalogs.md)
- [Integrating matching API](/C:/Users/xkodi/OJE/docs/integrating-matching-api.md)
