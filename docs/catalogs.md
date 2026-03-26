# Catalogs

OpenJobEngine no requiere seed de base de datos para taxonomias de skills, idiomas o ubicaciones.

Esos catalogos viven como archivos JSON versionados en:

- `src/OpenJobEngine.Infrastructure/Catalog/Data/skills.json`
- `src/OpenJobEngine.Infrastructure/Catalog/Data/languages.json`
- `src/OpenJobEngine.Infrastructure/Catalog/Data/locations.json`
- `src/OpenJobEngine.Infrastructure/Matching/Data/matching-rules.json`

## Por que asi

- evita pasos de seed para empezar
- facilita contribuciones por PR
- mantiene el conocimiento base auditable y versionado
- separa datos dinamicos de negocio de catalogos estaticos

## Que va en DB y que no

Va en DB:
- vacantes
- perfiles
- skills detectadas por vacante
- skills del candidato
- alerts
- saved searches
- ejecuciones

No va en DB:
- taxonomia base de lenguajes
- frameworks
- databases
- cloud/tools
- catalogo base de idiomas
- ubicaciones conocidas del sistema
- reglas base del matching

## Skills mas ricas

El catalogo de skills soporta:
- `tokens`
- `aliases`
- `equivalents`
- `related`

Eso permite enrichment mas preciso y matching parcial razonable sin depender de seeds.
