# Versioning and Releases

OpenJobEngine uses Semantic Versioning with Git Flow.

## Current baseline

- Current working version: `0.1.1-demo.2`
- Current release stage: `demo`
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

For the current demo cycle:

```xml
<VersionPrefix>0.1.1</VersionPrefix>
<VersionSuffix>demo.2</VersionSuffix>
```

To move to beta:

```xml
<VersionPrefix>0.2.0</VersionPrefix>
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
7. Publish the release notes in the annotated tag or GitHub release body using the same version tag.
