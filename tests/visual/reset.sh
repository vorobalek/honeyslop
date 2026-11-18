#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
DATABASE="${VISUAL_DATABASE:-honeyslop_visual}"
CONNECTION="Server=localhost;Port=5432;Database=$DATABASE;User Id=honeyslop;Password=honeyslop"
COMPOSE=(docker compose -f "$ROOT/docker-compose.postgres.yaml")
"${COMPOSE[@]}" exec -T postgres dropdb --if-exists --force "$DATABASE"
"${COMPOSE[@]}" exec -T postgres createdb -O honeyslop "$DATABASE"
DB_CONNECTION_STRING="$CONNECTION" dotnet run --no-launch-profile --project "$ROOT/src/Honeyslop.Migrator" > /dev/null
cd "$ROOT/tests/visual/seed"
dotnet run Seed.cs -- "$CONNECTION"
