#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
DATA_DIR="${REPO_ROOT}/data"
SQLITE_PATH="${DATA_DIR}/openjobengine.db"

mkdir -p "${DATA_DIR}"

export Persistence__Provider="${Persistence__Provider:-Sqlite}"
export ConnectionStrings__Sqlite="${ConnectionStrings__Sqlite:-Data Source=${SQLITE_PATH}}"

echo "Starting OpenJobEngine API"
echo "Persistence provider: ${Persistence__Provider}"
echo "SQLite database: ${SQLITE_PATH}"

dotnet run --project "${REPO_ROOT}/src/OpenJobEngine.Api"
