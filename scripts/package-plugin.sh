#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'USAGE'
Usage:
  scripts/package-plugin.sh <plugin-root> [output-dir]

Creates an Admin Shell plugin package with this structure:

  <plugin-id>.adminshell-plugin.zip
    manifest.json
    backend/
      <PluginName>.dll
      dependencias/
    frontend/ (when FrontEnd has built output)

Plugin source layout:

  plugins/MyPlugin/
    manifest.json
    Backend/
      MyPlugin.csproj
    FrontEnd/ (optional)
      package.json
      src/
      dist/

Examples:
  scripts/package-plugin.sh plugins/OrderCreationPlugin
  scripts/package-plugin.sh plugins/OrderCreationPlugin dist/plugins
USAGE
}

if [ "$#" -lt 1 ] || [ "$#" -gt 2 ]; then
  usage >&2
  exit 64
fi

PLUGIN_ROOT="${1%/}"
OUTPUT_DIR="${2:-dist/plugins}"

if [ ! -d "$PLUGIN_ROOT" ]; then
  echo "Plugin root not found: $PLUGIN_ROOT" >&2
  exit 66
fi

MANIFEST_JSON="$PLUGIN_ROOT/manifest.json"
BACKEND_DIR="$PLUGIN_ROOT/Backend"
FRONTEND_DIR="$PLUGIN_ROOT/FrontEnd"

if [ ! -f "$MANIFEST_JSON" ]; then
  echo "Manifest not found: $MANIFEST_JSON" >&2
  exit 66
fi

if [ ! -d "$BACKEND_DIR" ]; then
  echo "Backend directory not found: $BACKEND_DIR" >&2
  exit 66
fi

shopt -s nullglob
BACKEND_PROJECTS=("$BACKEND_DIR"/*.csproj)
shopt -u nullglob

if [ "${#BACKEND_PROJECTS[@]}" -ne 1 ]; then
  echo "Expected exactly one .csproj in $BACKEND_DIR, found ${#BACKEND_PROJECTS[@]}." >&2
  exit 66
fi

BACKEND_PROJECT="${BACKEND_PROJECTS[0]}"
PLUGIN_ID="$(python3 - "$MANIFEST_JSON" <<'PY'
import json
import sys
with open(sys.argv[1], encoding='utf-8') as file:
    manifest = json.load(file)
plugin_id = manifest.get('id')
if not plugin_id:
    raise SystemExit('manifest id is required')
print(plugin_id)
PY
)"

if ! command -v zip >/dev/null 2>&1; then
  echo "zip command is required" >&2
  exit 69
fi

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

WORK_DIR="$(mktemp -d)"
trap 'rm -rf "$WORK_DIR"' EXIT

PACKAGE_DIR="$WORK_DIR/package"
BACKEND_OUT="$PACKAGE_DIR/backend"
FRONTEND_OUT="$PACKAGE_DIR/frontend"
mkdir -p "$BACKEND_OUT" "$FRONTEND_OUT"

cp "$MANIFEST_JSON" "$PACKAGE_DIR/manifest.json"

echo "Publishing backend for $PLUGIN_ID..."
dotnet publish "$BACKEND_PROJECT" -c Release -o "$BACKEND_OUT" >/dev/null

PLUGIN_ASSEMBLY="$(basename "$BACKEND_PROJECT" .csproj)"
DEPENDENCIES_OUT="$BACKEND_OUT/dependencias"
mkdir -p "$DEPENDENCIES_OUT"

shopt -s nullglob
for file in "$BACKEND_OUT"/*; do
  base="$(basename "$file")"
  ext="${base##*.}"
  [ "$base" = "$PLUGIN_ASSEMBLY.dll" ] && continue
  [ "$base" = "$PLUGIN_ASSEMBLY.pdb" ] && continue
  case "$ext" in
    dll|pdb) cp "$file" "$DEPENDENCIES_OUT/" ;;
  esac
done
for file in "$BACKEND_OUT"/*; do
  base="$(basename "$file")"
  ext="${base##*.}"
  [ "$base" = "$PLUGIN_ASSEMBLY.dll" ] && continue
  [ "$base" = "$PLUGIN_ASSEMBLY.pdb" ] && continue
  case "$ext" in
    dll|pdb) rm -f "$file" ;;
  esac
done
shopt -u nullglob

if [ -d "$FRONTEND_DIR" ] && [ -f "$FRONTEND_DIR/package.json" ]; then
  if [ -f "$FRONTEND_DIR/package-lock.json" ]; then
    npm ci --prefix "$FRONTEND_DIR" >/dev/null
  else
    npm install --prefix "$FRONTEND_DIR" >/dev/null
  fi
  npm run build:frontend --prefix "$FRONTEND_DIR" >/dev/null
fi

if [ -d "$FRONTEND_DIR/dist" ]; then
  DIST_DIR="$FRONTEND_DIR/dist"
elif [ -f "$FRONTEND_DIR/index.js" ]; then
  DIST_DIR="$FRONTEND_DIR"
else
  DIST_DIR=""
fi

if [ -n "$DIST_DIR" ]; then
  echo "Packaging frontend from $DIST_DIR..."
  if [ "$DIST_DIR" = "$FRONTEND_DIR" ]; then
    cp "$FRONTEND_DIR/index.js" "$FRONTEND_OUT/index.js"
    if [ -f "$FRONTEND_DIR/styles.css" ]; then
      cp "$FRONTEND_DIR/styles.css" "$FRONTEND_OUT/styles.css"
    fi
  else
    for item in "$DIST_DIR"/*; do
      [ -e "$item" ] || continue
      [ "$(basename "$item")" = "plugin.json" ] && continue
      cp -R "$item" "$FRONTEND_OUT"/
    done
  fi
fi

mkdir -p "$OUTPUT_DIR"
ZIP_PATH="$ROOT_DIR/$OUTPUT_DIR/$PLUGIN_ID.adminshell-plugin.zip"
rm -f "$ZIP_PATH"

shopt -s nullglob
FRONTEND_FILES=("$FRONTEND_OUT"/*)
shopt -u nullglob

if [ "${#FRONTEND_FILES[@]}" -gt 0 ]; then
  (cd "$PACKAGE_DIR" && zip -qr "$ZIP_PATH" manifest.json backend frontend)
else
  (cd "$PACKAGE_DIR" && zip -qr "$ZIP_PATH" manifest.json backend)
fi

echo "Created $ZIP_PATH"
