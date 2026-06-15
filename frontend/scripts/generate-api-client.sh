#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
FRONTEND_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
REPO_ROOT="$(cd "$FRONTEND_ROOT/.." && pwd)"
CONFIGURATION="${CONFIGURATION:-Debug}"
API_URL="${ADMINSHELL_API_URL:-http://127.0.0.1:5000}"
OPENAPI_URL="${ADMINSHELL_OPENAPI_URL:-${API_URL%/}/openapi/v1.json}"
OPENAPI_FILE="$FRONTEND_ROOT/src/generated/openapi/adminshell.json"
HOST_PLUGINS_DIR="$REPO_ROOT/backend/AdminShell.Host/plugins"
LOG_FILE="${TMPDIR:-/tmp}/adminshell-openapi-generation.log"

if [ -n "${DOTNET_ROOT:-}" ] && [ -x "$DOTNET_ROOT/dotnet" ]; then
  DOTNET_BIN="$DOTNET_ROOT/dotnet"
elif command -v dotnet >/dev/null 2>&1; then
  DOTNET_BIN="$(command -v dotnet)"
else
  echo "dotnet executable not found. Set DOTNET_ROOT or add dotnet to PATH." >&2
  exit 1
fi

mkdir -p "$(dirname "$OPENAPI_FILE")"

prepare_plugin_assemblies() {
  local project plugin_dir plugin_name output_dir dest

  rm -rf "$HOST_PLUGINS_DIR"
  mkdir -p "$HOST_PLUGINS_DIR"

  shopt -s nullglob
  for project in "$REPO_ROOT"/plugins/*/Backend/*.csproj; do
    plugin_dir="$(dirname "$project")"
    plugin_name="$(basename "$(dirname "$plugin_dir")")"

    echo "Building plugin backend: $plugin_name"
    "$DOTNET_BIN" build "$project" -c "$CONFIGURATION"

    output_dir=""
    for candidate in "$plugin_dir"/bin/"$CONFIGURATION"/net*/; do
      if [ -d "$candidate" ]; then
        output_dir="$candidate"
      fi
    done

    if [ -z "$output_dir" ]; then
      echo "Could not find plugin output for $plugin_name under $plugin_dir/bin/$CONFIGURATION/net*/" >&2
      exit 1
    fi

    dest="$HOST_PLUGINS_DIR/$plugin_name"
    mkdir -p "$dest"
    cp -a "$output_dir/$plugin_name.dll" "$dest/"
    if [ -f "$output_dir/$plugin_name.pdb" ]; then
      cp -a "$output_dir/$plugin_name.pdb" "$dest/"
    fi
    if [ -f "$output_dir/$plugin_name.deps.json" ]; then
      cp -a "$output_dir/$plugin_name.deps.json" "$dest/"
    fi
  done
  shopt -u nullglob
}

prepare_plugin_assemblies

if curl -fsS "$OPENAPI_URL" -o "$OPENAPI_FILE"; then
  echo "Using existing AdminShell API at $API_URL"
else
  echo "OpenAPI endpoint not reachable at $OPENAPI_URL; starting backend temporarily..."

  export ADMIN_SHELL_SKIP_TS_CLIENT_GEN=true
  "$DOTNET_BIN" run \
    --project "$REPO_ROOT/backend/AdminShell.Host/AdminShell.Host.csproj" \
    --no-launch-profile \
    --urls "$API_URL" >"$LOG_FILE" 2>&1 &
  BACKEND_PID=$!

  cleanup() {
    if kill -0 "$BACKEND_PID" 2>/dev/null; then
      kill "$BACKEND_PID" 2>/dev/null || true
      wait "$BACKEND_PID" 2>/dev/null || true
    fi
  }
  trap cleanup EXIT

  for _ in $(seq 1 90); do
    if curl -fsS "$OPENAPI_URL" -o "$OPENAPI_FILE"; then
      break
    fi

    if ! kill -0 "$BACKEND_PID" 2>/dev/null; then
      echo "Backend failed while generating OpenAPI. Log:" >&2
      cat "$LOG_FILE" >&2
      exit 1
    fi

    sleep 1
  done

  if [ ! -s "$OPENAPI_FILE" ]; then
    echo "Timed out waiting for $OPENAPI_URL" >&2
    cat "$LOG_FILE" >&2
    exit 1
  fi
fi

node "$FRONTEND_ROOT/scripts/generate-api-client.mjs" --openapi-file "$OPENAPI_FILE"
