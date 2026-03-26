#!/usr/bin/env bash
set -euo pipefail

COMMAND="${1:-}"
NAME="${2:-}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"

require_git_flow() {
  if ! git flow version >/dev/null 2>&1; then
    echo "git-flow is required. Install git-flow-next before using this script." >&2
    exit 1
  fi
}

current_branch() {
  git rev-parse --abbrev-ref HEAD
}

ensure_clean_working_tree() {
  if [[ -n "$(git status --porcelain)" ]]; then
    echo "Working tree is not clean. Commit or stash your changes before running '${COMMAND}'." >&2
    exit 1
  fi
}

ensure_develop_exists() {
  if ! git show-ref --verify --quiet refs/heads/develop; then
    git branch develop main
  fi

  if ! git ls-remote --exit-code --heads origin develop >/dev/null 2>&1; then
    git push -u origin develop
  fi
}

run_verification() {
  (
    cd "${REPO_ROOT}"
    dotnet restore OpenJobEngine.sln
    dotnet build OpenJobEngine.sln -c Release
  )
}

branch_suffix() {
  local prefix="$1"
  local branch
  branch="$(current_branch)"

  if [[ "${branch}" != ${prefix}* ]]; then
    echo "Current branch must start with '${prefix}'." >&2
    exit 1
  fi

  echo "${branch#${prefix}}"
}

case "${COMMAND}" in
  init)
    require_git_flow
    ensure_develop_exists
    if [[ "$(git config --local --get gitflow.initialized 2>/dev/null || true)" == "true" ]]; then
      echo "git-flow is already initialized in this repository."
      exit 0
    fi
    git flow init --preset=classic --main main --develop develop --tag v
    ;;

  feature-start)
    if [[ -z "${NAME}" ]]; then
      echo "Feature name is required." >&2
      exit 1
    fi
    ensure_clean_working_tree
    require_git_flow
    git checkout develop
    git fetch origin
    git pull --ff-only origin develop
    git flow feature start "${NAME}"
    ;;

  feature-sync)
    require_git_flow
    if [[ "$(current_branch)" != feature/* ]]; then
      echo "feature-sync must run from a feature branch." >&2
      exit 1
    fi
    ensure_clean_working_tree
    git fetch origin
    git rebase origin/develop
    ;;

  feature-finish)
    require_git_flow
    ensure_clean_working_tree
    run_verification
    if [[ -z "${NAME}" ]]; then
      NAME="$(branch_suffix "feature/")"
    fi
    git flow feature finish "${NAME}"
    echo "Feature finished. Push develop with: git push origin develop"
    ;;

  release-start)
    if [[ -z "${NAME}" ]]; then
      echo "Release version is required." >&2
      exit 1
    fi
    ensure_clean_working_tree
    require_git_flow
    git checkout develop
    git fetch origin
    git pull --ff-only origin develop
    git flow release start "${NAME}"
    ;;

  release-finish)
    require_git_flow
    ensure_clean_working_tree
    run_verification
    if [[ -z "${NAME}" ]]; then
      NAME="$(branch_suffix "release/")"
    fi
    git flow release finish "${NAME}"
    echo "Release finished. Push with:"
    echo "  git push origin develop"
    echo "  git push origin main --tags"
    ;;

  hotfix-start)
    if [[ -z "${NAME}" ]]; then
      echo "Hotfix version is required." >&2
      exit 1
    fi
    ensure_clean_working_tree
    require_git_flow
    git checkout main
    git fetch origin
    git pull --ff-only origin main
    git flow hotfix start "${NAME}"
    ;;

  hotfix-finish)
    require_git_flow
    ensure_clean_working_tree
    run_verification
    if [[ -z "${NAME}" ]]; then
      NAME="$(branch_suffix "hotfix/")"
    fi
    git flow hotfix finish "${NAME}"
    echo "Hotfix finished. Push with:"
    echo "  git push origin develop"
    echo "  git push origin main --tags"
    ;;

  *)
    echo "Unknown command '${COMMAND}'. Supported commands: init, feature-start, feature-sync, feature-finish, release-start, release-finish, hotfix-start, hotfix-finish." >&2
    exit 1
    ;;
esac
