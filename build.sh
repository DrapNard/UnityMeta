#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$ROOT"

dotnet restore src/UnityMeta.Runtime/UnityMeta.Runtime.csproj
dotnet restore src/UnityMeta.Weaver.Core/UnityMeta.Weaver.Core.csproj
dotnet restore compiler/UnityMeta.Compiler/UnityMeta.Compiler.csproj
dotnet restore tests/UnityMeta.SmokeTests/UnityMeta.SmokeTests.csproj

dotnet build src/UnityMeta.Runtime/UnityMeta.Runtime.csproj -c Release --no-restore
dotnet build src/UnityMeta.Weaver.Core/UnityMeta.Weaver.Core.csproj -c Release --no-restore
dotnet build compiler/UnityMeta.Compiler/UnityMeta.Compiler.csproj -c Release --no-restore
dotnet run --project tests/UnityMeta.SmokeTests/UnityMeta.SmokeTests.csproj -c Release --no-restore

echo "UnityMeta build and smoke tests completed."
