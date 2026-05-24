#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PID_DIR="$ROOT_DIR/.local/pids"

if [[ ! -d "$PID_DIR" ]]; then
  echo "No local instances found."
  exit 0
fi

for pid_file in "$PID_DIR"/websocketpubsub-*.pid; do
  [[ -e "$pid_file" ]] || continue

  pid="$(cat "$pid_file")"

  if kill -0 "$pid" 2>/dev/null; then
    kill "$pid"
    echo "Stopped process $pid"
  fi

  rm -f "$pid_file"
done
