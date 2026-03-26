# Adding Providers

## Regla principal

Un provider en OpenJobEngine solo recolecta datos y los transforma a `RawJobOffer`.

No debe:
- acceder a base de datos
- ejecutar deduplicacion
- aplicar enrichment
- decidir matching

Todo eso ocurre despues en el pipeline:

```text
Provider -> RawJobOffer -> Normalization -> Enrichment -> Deduplication -> Persistence -> History
```

## Pasos

1. Implementa `IJobProvider` en infraestructura.
2. Crea una clase de opciones para habilitarlo y parametrizarlo.
3. Usa `HttpClient` o Playwright segun el origen.
4. Devuelve `IReadOnlyCollection<RawJobOffer>`.
5. Registra el provider de forma condicional en `AddOpenJobEngineInfrastructure`.

## Checklist

- `SourceName` estable
- `SourceJobId` consistente
- `Url` absoluta
- `PublishedAtUtc` cuando exista
- `Metadata` con datos utiles del origen
- descripciones razonablemente limpias
- ubicacion y salario en `Metadata` cuando el origen los expone

## Recomendacion actual

Para nuevos providers, prioriza fuentes estructuradas tipo API o ATS JSON antes que scraping fragil.

Ejemplo de referencia en esta base:
- `Greenhouse`
