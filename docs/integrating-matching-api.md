# Integrating Matching API

## Objetivo

Esta guia resume como integrar OpenJobEngine como backend-first para matching de candidatos.

## Flujo recomendado

1. Crear perfil:

```http
POST /api/profiles
```

2. Subir CV opcional para prellenado:

```http
POST /api/profiles/{profileId}/resume
Content-Type: multipart/form-data
```

La respuesta incluye:
- `textPreview`
- `suggestedImport.suggestedProfile`
- `suggestedImport.fieldConfidences`
- `suggestedImport.detectedSections`
- `suggestedImport.detectedSkills`
- `suggestedImport.detectedLanguages`
- `suggestedImport.estimatedYearsOfExperience`
- `warnings`

3. Ajustar skills, idiomas o preferencias si hace falta:

```http
PATCH /api/profiles/{profileId}/skills
PATCH /api/profiles/{profileId}/languages
PATCH /api/profiles/{profileId}/preferences
```

4. Pedir matches:

```http
POST /api/matching/search
```

5. Inspeccionar reglas activas:

```http
GET /api/matching/rules
```

6. Revisar historico de una vacante:

```http
GET /api/jobs/{jobId}/history
```

## Respuesta de matching

Cada match devuelve:
- `matchScore`
- `matchBand`
- `ruleVersion`
- `matchReasons`
- `missingRequirements`
- `salaryFit`
- `locationFit`
- `languageFit`
- la vacante enriquecida

## Decisiones del core

- No hay autenticacion incluida en esta capa.
- El `profileId` identifica el perfil desde el backend.
- El parsing de CV no muta el perfil implicitamente salvo que `applyToProfile` sea `true`.
- El ranking es explicable y configurable desde `matching-rules.json`.
- Las taxonomias de skills, idiomas y ubicaciones viven versionadas en el repo.
