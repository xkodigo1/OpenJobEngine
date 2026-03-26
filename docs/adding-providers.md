# Adding Providers

## Regla principal

Un provider en OpenJobEngine sólo recolecta datos y los transforma a `RawJobOffer`. La normalización, deduplicación y persistencia viven fuera del provider.

## Pasos

1. Implementa `IJobProvider` en `OpenJobEngine.Infrastructure`.
2. Crea una clase de opciones para habilitarlo y parametrizarlo.
3. Usa `HttpClient` o Playwright según el origen.
4. Devuelve `IReadOnlyCollection<RawJobOffer>`.
5. Registra el provider de forma condicional en `AddOpenJobEngineInfrastructure`.

## Checklist

- `SourceName` estable
- `SourceJobId` consistente
- `Url` absoluta
- `PublishedAtUtc` cuando exista
- `Metadata` con datos útiles del origen
