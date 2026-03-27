#!/usr/bin/env bash
set -euo pipefail

VERSION=""
CHANGELOG_PATH=""
OUTPUT_PATH=""

show_help() {
  cat <<'EOF'
Usage:
  export-release-notes.sh --version <version> [--changelog <path>] [--output <path>]

Examples:
  ./scripts/export-release-notes.sh --version 0.4.0-beta.1 --output artifacts/release-notes/v0.4.0-beta.1.md
EOF
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --version|-v)
      VERSION="${2:-}"
      shift 2
      ;;
    --changelog|-c)
      CHANGELOG_PATH="${2:-}"
      shift 2
      ;;
    --output|-o)
      OUTPUT_PATH="${2:-}"
      shift 2
      ;;
    --help|-h)
      show_help
      exit 0
      ;;
    *)
      echo "Unknown argument: $1" >&2
      show_help >&2
      exit 1
      ;;
  esac
done

if [[ -z "${VERSION}" ]]; then
  echo "Missing required --version argument." >&2
  show_help >&2
  exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
CHANGELOG_PATH="${CHANGELOG_PATH:-${REPO_ROOT}/CHANGELOG.md}"
VERSION="${VERSION#v}"

if [[ ! -f "${CHANGELOG_PATH}" ]]; then
  echo "CHANGELOG file not found at '${CHANGELOG_PATH}'." >&2
  exit 1
fi

section="$(awk -v version="${VERSION}" '
  BEGIN { found=0 }
  index($0, "## [" version "]") == 1 {
    found=1
    print
    next
  }
  found && index($0, "## [") == 1 {
    exit
  }
  found {
    print
  }
' "${CHANGELOG_PATH}")"

if [[ -z "${section}" ]]; then
  echo "Release notes for version '${VERSION}' were not found in '${CHANGELOG_PATH}'." >&2
  exit 1
fi

if [[ -n "${OUTPUT_PATH}" ]]; then
  mkdir -p "$(dirname "${OUTPUT_PATH}")"
  printf '%s\n' "${section}" > "${OUTPUT_PATH}"
  echo "Release notes exported to ${OUTPUT_PATH}"
else
  printf '%s\n' "${section}"
fi
