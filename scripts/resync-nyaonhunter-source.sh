#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PS_SCRIPT_WIN="\\\\wsl.localhost\\Ubuntu${SCRIPT_DIR//\//\\}\\resync-nyaonhunter-source.ps1"

exec powershell.exe -NoProfile -ExecutionPolicy Bypass -File "$PS_SCRIPT_WIN" "$@"
