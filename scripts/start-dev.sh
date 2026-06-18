#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
LOG_DIR="${ROOT_DIR}/.dev/logs"

mkdir -p "${LOG_DIR}"

export PATH="${HOME}/.dotnet:${PATH}"
export ADMIN_SHELL_SKIP_TS_CLIENT_GEN="${ADMIN_SHELL_SKIP_TS_CLIENT_GEN:-true}"

BACKEND_URL="${BACKEND_URL:-http://127.0.0.1:5000}"
FRONTEND_HOST="${FRONTEND_HOST:-127.0.0.1}"
FRONTEND_PORT="${FRONTEND_PORT:-5173}"
export VITE_API_BASE_URL="${VITE_API_BASE_URL:-${BACKEND_URL}}"

BACKEND_PID=""
FRONTEND_PID=""

cleanup() {
  if [[ -n "${BACKEND_PID}" ]] && kill -0 "${BACKEND_PID}" 2>/dev/null; then
    kill "${BACKEND_PID}" 2>/dev/null || true
  fi

  if [[ -n "${FRONTEND_PID}" ]] && kill -0 "${FRONTEND_PID}" 2>/dev/null; then
    kill "${FRONTEND_PID}" 2>/dev/null || true
  fi
}

trap cleanup EXIT INT TERM

start_backend() {
  echo "Starting backend on ${BACKEND_URL}..."
  (
    cd "${ROOT_DIR}"
    dotnet run \
      --project backend/AdminShell.Host/AdminShell.Host.csproj \
      --no-launch-profile \
      --urls "${BACKEND_URL}"
  ) >"${LOG_DIR}/backend.log" 2>&1 &

  BACKEND_PID="$!"
  echo "Backend PID: ${BACKEND_PID}"
  echo "Backend log: ${LOG_DIR}/backend.log"
}

start_frontend() {
  echo "Starting frontend on http://${FRONTEND_HOST}:${FRONTEND_PORT}..."
  (
    cd "${ROOT_DIR}/frontend"
    npm run dev -- --host "${FRONTEND_HOST}" --port "${FRONTEND_PORT}"
  ) >"${LOG_DIR}/frontend.log" 2>&1 &

  FRONTEND_PID="$!"
  echo "Frontend PID: ${FRONTEND_PID}"
  echo "Frontend log: ${LOG_DIR}/frontend.log"
}

start_backend
start_frontend

echo ""
echo "Admin shell dev is running:"
echo "  Backend:  ${BACKEND_URL}"
echo "  Frontend: http://${FRONTEND_HOST}:${FRONTEND_PORT}"
echo ""
echo "Logs:"
echo "  backend:  ${LOG_DIR}/backend.log"
echo "  frontend: ${LOG_DIR}/frontend.log"
echo ""
echo "Press Ctrl+C to stop both processes."

wait -n "${BACKEND_PID}" "${FRONTEND_PID}"
