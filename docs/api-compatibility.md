# API Compatibility Policy

This repository is not enforcing a full contract-diff pipeline yet, but it does follow a practical compatibility discipline for the public backend API.

## Public surface

The public surface is the set of endpoints under `/api` plus the version and problem-details conventions exposed by the API.

## Compatibility rules

- add fields before removing or renaming fields
- prefer new endpoints over changing the shape of an existing released endpoint
- keep `application/problem+json` stable for error responses
- keep the version header and Swagger version aligned with the tagged release

## Breaking changes

A change is breaking if it:

- removes or renames a public route
- changes a required request field
- changes the meaning of a required response field
- changes error envelopes in a way that breaks existing consumers

If a breaking change is unavoidable:

- document it in `CHANGELOG.md`
- call it out in the exported release notes
- cut a version that makes the change explicit

## Release checklist

Before tagging a release:

1. review the public endpoints in Swagger
2. confirm the version exposed by the API matches the release tag
3. export release notes from `CHANGELOG.md`
4. verify any public API change is called out in the notes
5. avoid silent contract changes

## Practical scope after v0.4.0

`v0.4.0` establishes the first explicit compatibility discipline, but it still avoids a heavy contract-diff toolchain.

The repo should be consistent enough that future releases can tighten this further with:

- OpenAPI snapshots
- explicit compatibility diffs
- deprecation warnings before removals
