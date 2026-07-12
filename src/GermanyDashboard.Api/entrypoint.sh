#!/bin/sh
# Applies pending EF Core migrations and seeds the demo dataset if the database is empty
# (DbSeeder no-ops when Regions already has rows), then starts the API. Safe to run on
# every container start/restart.
set -e

dotnet GermanyDashboard.Api.dll seed

exec dotnet GermanyDashboard.Api.dll
