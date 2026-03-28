# Release Notes Workflow

`CHANGELOG.md` is the canonical source for release notes.

Release notes for a tag or GitHub Release should be exported from the changelog instead of being authored as ad-hoc documents inside the repo.

## Exporting notes

PowerShell:

```powershell
.\scripts\export-release-notes.ps1 -Version 1.0.0 -OutputPath artifacts\release-notes\v1.0.0.md
```

Linux/macOS:

```bash
./scripts/export-release-notes.sh --version 1.0.0 --output artifacts/release-notes/v1.0.0.md
```

## Expected output

The exported markdown should contain:

- the version heading from `CHANGELOG.md`
- the section body for that release
- only the public-facing changes that belong in the tag or GitHub Release body

## Publishing rule

- use the exported markdown as the tag annotation body or GitHub Release body
- keep long-lived release history in `CHANGELOG.md`
- avoid separate per-release notes files unless a release needs a temporary draft artifact
