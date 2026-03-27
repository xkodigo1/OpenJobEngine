# Versioning and Releases

OpenJobEngine uses Semantic Versioning with Git Flow.

## Current baseline

- Current working version: `0.3.0-beta.3`
- Current release stage: `beta`
- Tag format: `v<version>`

Examples:

- `v0.1.0-demo.1`
- `v0.1.0-beta.1`
- `v0.1.0`
- `v0.1.1`
- `v0.2.0`

## Release stages

### Demo

Use demo builds to validate the product in controlled presentations or early internal reviews.

Pattern:

- `0.1.0-demo.1`
- `0.1.0-demo.2`

Rules:

- breaking changes are still allowed
- contracts may still move
- release notes should focus on what can be shown in a demo

### Beta

Use beta builds once the API shape and main flows are stable enough for external validation.

Pattern:

- `0.1.0-beta.1`
- `0.1.0-beta.2`

Rules:

- no major architecture pivots without explicit decision
- prioritize compatibility, bug fixes and operability
- document known limitations clearly

### Stable

Use stable releases when the API and operational behavior are considered publishable.

Pattern:

- `0.1.0`
- `0.1.1`
- `0.2.0`
- `1.0.0`

Rules:

- patch: fixes and low-risk improvements
- minor: backwards-compatible features
- major: breaking changes

## Git Flow mapping

### Feature work

- branch from `develop`
- branch name: `feature/<name>`
- merge back into `develop`

### Release branches

Use release branches for version bumps, changelog updates and final polish only.

Examples:

```bash
git flow release start 0.1.0-demo.1
git flow release finish 0.1.0-demo.1
git push origin develop
git push origin main --tags
```

```bash
git flow release start 0.1.0-beta.1
git flow release finish 0.1.0-beta.1
git push origin develop
git push origin main --tags
```

Important:

- because the repo already uses tag prefix `v`, the release name should be `0.1.0-demo.1`, not `v0.1.0-demo.1`
- the resulting tag will be `v0.1.0-demo.1`
- release notes should be exported from `CHANGELOG.md` and attached to the tag or GitHub Release body

Release notes export:

```powershell
.\scripts\export-release-notes.ps1 -Version 0.4.0-beta.1 -OutputPath artifacts\release-notes\v0.4.0-beta.1.md
```

```bash
./scripts/export-release-notes.sh --version 0.4.0-beta.1 --output artifacts/release-notes/v0.4.0-beta.1.md
```

### Hotfix branches

Use hotfix branches only from `main` for urgent production fixes.

Example:

```bash
git flow hotfix start 0.1.1
git flow hotfix finish 0.1.1
git push origin develop
git push origin main --tags
```

## How to bump versions

Versioning is centralized in `Directory.Build.props`.

Default properties:

- `VersionPrefix`
- `VersionSuffix`
- `Version`
- `PackageVersion`
- `AssemblyVersion`
- `FileVersion`
- `InformationalVersion`

For the current beta line:

```xml
<VersionPrefix>0.3.0</VersionPrefix>
<VersionSuffix>beta.3</VersionSuffix>
```

To move to the next beta iteration:

```xml
<VersionPrefix>0.4.0</VersionPrefix>
<VersionSuffix>beta.1</VersionSuffix>
```

To publish the first stable:

```xml
<VersionPrefix>0.1.0</VersionPrefix>
<VersionSuffix></VersionSuffix>
```

To publish the next patch:

```xml
<VersionPrefix>0.1.1</VersionPrefix>
<VersionSuffix></VersionSuffix>
```

## Release checklist

1. Ensure the branch is based on the correct Git Flow branch.
2. Update `Directory.Build.props` with the target version.
3. Update `CHANGELOG.md`.
4. Run:

```bash
dotnet restore OpenJobEngine.sln
dotnet build OpenJobEngine.sln -c Release
```

5. Finish the release or hotfix branch.
6. Push `develop`, `main` and tags.
7. Export release notes from `CHANGELOG.md` and publish them in the annotated tag or GitHub release body using the same version tag.
8. Record any public API change in `docs/api-compatibility.md` and call it out explicitly in the release notes.

## API compatibility discipline

OpenJobEngine is still in the `0.x` line, but public API changes should be handled deliberately:

- additive fields are preferred over renames
- new endpoints should preserve existing contracts
- response envelopes should stay stable once released
- breaking changes must be documented in `CHANGELOG.md` and the release notes

Pragmatic rule for this repo:

- if an API change affects current consumers, treat it as a release decision, not an implementation detail
- use `docs/api-compatibility.md` as the short checklist before cutting a release
