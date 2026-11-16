#!/bin/bash

BASEDIR=$(dirname "$0")
set -x
cd "$BASEDIR/../src/Honeyslop.Migrator" || exit
(dotnet ef migrations remove || true)
exit
