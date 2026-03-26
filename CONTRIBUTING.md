# Contributing

## Principios

- Respeta la arquitectura por capas.
- No añadas lógica de negocio en controllers ni providers.
- Los providers sólo recolectan y parsean `RawJobOffer`.
- Mantén los cambios pequeños y documentados.

## Flujo recomendado

1. Crea una rama desde `main`.
2. Implementa cambios por capa.
3. Documenta cualquier provider nuevo en `docs/adding-providers.md`.
4. Ejecuta verificaciones manuales antes de abrir PR.

## Convenciones

- `Domain` no depende de otras capas.
- `Application` define contratos y casos de uso.
- `Infrastructure` implementa adaptadores externos.
- `Api` y `Worker` sólo orquestan mediante DI.
