#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROJECT="$ROOT_DIR/samples/WebsocketPubsub/WebsocketPubsub.csproj"
RUNTIME_DIR="$ROOT_DIR/.local"
PID_DIR="$RUNTIME_DIR/pids"
LOG_DIR="$RUNTIME_DIR/logs"
PORTS=(5232 5233 5234)

mkdir -p "$PID_DIR" "$LOG_DIR"

docker compose -f "$ROOT_DIR/docker-compose.yml" up -d
dotnet restore "$PROJECT" -p:NuGetAudit=false
dotnet build "$PROJECT" --no-restore -p:NuGetAudit=false

for port in "${PORTS[@]}"; do
  pid_file="$PID_DIR/websocketpubsub-$port.pid"
  log_file="$LOG_DIR/websocketpubsub-$port.log"

  if [[ -f "$pid_file" ]] && kill -0 "$(cat "$pid_file")" 2>/dev/null; then
    echo "WebsocketPubsub is already running on http://localhost:$port"
    continue
  fi

  ASPNETCORE_ENVIRONMENT=Development \
    dotnet run --project "$PROJECT" --no-build --urls "http://localhost:$port" \
    >"$log_file" 2>&1 &

  echo "$!" > "$pid_file"
  echo "Started WebsocketPubsub on http://localhost:$port"
  echo "  log: $log_file"
done

echo
echo "Open these URLs in separate browser tabs:"
for port in "${PORTS[@]}"; do
  echo "  http://localhost:$port"
done
echo
echo "Stop with: make local-lb-stop"
