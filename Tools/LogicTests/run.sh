#!/usr/bin/env bash
# Compila e testa a lógica pura do jogo sem abrir a Unity.
set -euo pipefail
cd "$(dirname "$0")"
dotnet test Tests/Enxada.Logic.Tests.csproj "$@"
