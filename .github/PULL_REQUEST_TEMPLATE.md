## Summary

- What changed?
- Why was it needed?

## Git Flow

- Source branch follows Git Flow naming (`feature/*`, `release/*`, `hotfix/*`)
- Target branch is correct for this flow
- No direct work was done on `main` or `develop`

## Validation

- `dotnet restore OpenJobEngine.sln`
- `dotnet build OpenJobEngine.sln -c Release`

## Notes

- Docs updated if behavior or workflow changed
- Providers, matching rules or catalogs updated only where needed
