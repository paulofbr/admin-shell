#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

if ! command -v docker >/dev/null 2>&1; then
  echo "docker is required" >&2
  exit 1
fi

if docker ps --format '{{.Names}}' | grep -qx adminshell-sql; then
  echo "adminshell-sql already running"
else
  docker rm -f adminshell-sql >/dev/null 2>&1 || true
  docker run -d \
    --name adminshell-sql \
    -p 1434:1433 \
    -e ACCEPT_EULA=Y \
    -e MSSQL_SA_PASSWORD='Admin123!' \
    mcr.microsoft.com/mssql/server:2022-latest >/dev/null
  echo "started adminshell-sql on localhost,1434"
fi

cd "$ROOT_DIR/frontend"
npm install
npm run build

cd "$ROOT_DIR/plugins/OrderCreationPlugin/FrontEnd"
npm install
npm run build
