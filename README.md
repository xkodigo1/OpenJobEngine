# OpenJobEngine

OpenJobEngine es un backend open source para agregación de ofertas de empleo multi-fuente. Sigue una arquitectura limpia con capas separadas para dominio, aplicación, infraestructura, API y worker.

## MVP incluido

- Recolección desde múltiples providers desacoplados
- Normalización hacia un modelo unificado
- Deduplicación por clave canónica
- Persistencia en PostgreSQL con EF Core
- API REST para consulta y ejecución manual de colectas
- Worker opcional para ejecución periódica

## Estructura

```text
src/
  OpenJobEngine.Domain
  OpenJobEngine.Application
  OpenJobEngine.Infrastructure
  OpenJobEngine.Api
  OpenJobEngine.Worker
docs/
```

## Inicio rápido

1. Instala el SDK de .NET 8 o 9.
2. Crea una base PostgreSQL.
3. Copia `.env.example` a tu configuración local.
4. Ajusta `ConnectionStrings__Postgres`.
5. Habilita los providers que quieras usar en `appsettings`.
6. Ejecuta la API:

```bash
dotnet run --project src/OpenJobEngine.Api
```

7. Ejecuta el worker si quieres colección programada:

```bash
dotnet run --project src/OpenJobEngine.Worker
```

## Endpoints

- `GET /api/jobs`
- `GET /api/jobs/search`
- `GET /api/jobs/{id}`
- `POST /api/collections/run`
- `POST /api/collections/run/{source}`
- `GET /api/collections/executions`

## Providers incluidos

- `Computrabajo`: scraping HTML configurable
- `Adzuna`: integración por API

Los providers son opt-in y se registran por configuración.

## Agregar un provider nuevo

1. Implementa `IJobProvider`.
2. Devuelve únicamente `RawJobOffer`.
3. Registra el provider en `AddOpenJobEngineInfrastructure`.
4. Añade su configuración en `Providers`.

Más detalle en [docs/adding-providers.md](/C:/Users/xkodi/OJE/docs/adding-providers.md).

## Nota de entorno

El workspace actual no tiene SDK instalado, sólo runtime. El código queda preparado, pero la restauración de paquetes, build y migraciones deberán correrse cuando el SDK esté disponible.
