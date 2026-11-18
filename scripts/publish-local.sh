#!/bin/sh
set -eu
cd "$(dirname "$0")/.."
dotnet restore MESagent.sln --locked-mode
dotnet publish src/MESagent.Api -c Release --no-restore -p:UseAppHost=false -o artifacts/docker/api -m:1 -nr:false
dotnet publish src/MESagent.Database -c Release --no-restore -p:UseAppHost=false -o artifacts/docker/database -m:1 -nr:false
