# DACHInsights — Backend

ASP.NET Core (.NET 10) Web API for **DACHInsights**, a statistics dashboard covering Germany, Austria, and Switzerland. Clean Architecture layering, EF Core + PostgreSQL, real regional data pulled from Eurostat.

Pairs with the [frontend](https://github.com/duzzisantos/dach-insights-frontend) (Next.js). See that repo, or the [deployment repo](https://github.com/duzzisantos) `docker-compose.yml`/`DEPLOY.md`, for running both together.

## Architecture

```
src/
├── GermanyDashboard.Domain          # Entities: Region, Category, Indicator, DataPoint, DataSource
├── GermanyDashboard.Application     # DTOs + service interfaces
├── GermanyDashboard.Infrastructure  # EF Core, Postgres, seed data, Eurostat + GENESIS-Destatis clients
└── GermanyDashboard.Api             # Controllers, security middleware, Program.cs
tests/GermanyDashboard.Tests
```

Data covers all 35 regions: Germany (national + 16 NUTS-1 states), Austria (national + 9 NUTS-2 states), Switzerland (national + 7 NUTS-2 greater regions) — population, GDP per capita, unemployment rate, and life expectancy, 2015–2024.

**Known data gap:** Eurostat has no regional (sub-national) GDP data for Switzerland — it's an EU-only regional-accounts table. Switzerland's *national* GDP per capita is derived from a separate total-GDP series (÷ population) instead; its 7 greater regions have no GDP figure, and the frontend omits that stat/chart there rather than fabricate one.

## Prerequisites

- .NET SDK 10.x
- PostgreSQL running locally (or reachable elsewhere)

## Environment variables

Dev defaults live in `src/GermanyDashboard.Api/appsettings.Development.json` — override with real env vars in any shared/production environment.

| Variable | Required | Example | Notes |
|---|---|---|---|
| `ConnectionStrings__Default` | **Yes** | `Host=localhost;Port=5432;Database=germany_dashboard;Username=germany_dashboard;Password=...` | Postgres connection string. No default — app throws on startup if missing. |
| `Cors__AllowedOrigins__0` | **Yes** (prod) | `https://your-frontend-domain.com` | Repeat `__1`, `__2`, ... for multiple origins. Dev already has `http://localhost:3000` pre-configured. |
| `Destatis__Username` | No | — | Only needed to pull **real** GENESIS-Destatis data. Register at https://www-genesis.destatis.de/genesis/online |
| `Destatis__Password` | No | — | See above. Never commit this. |
| `Destatis__TableCodes__0` | No | `12411-0010` | GENESIS table code(s) you want to sync, once you know which tables your account can access. |

`appsettings.Development.json` is checked in on purpose — its password is a disposable local-only dev credential, not a real secret.

## Running it locally

```bash
# Postgres (adjust to however you run Postgres locally)
createuser germany_dashboard --pwprompt   # set password: localdev_change_me
createdb germany_dashboard --owner=germany_dashboard

# Applies migrations + seeds demo data automatically in Development
cd src/GermanyDashboard.Api
ASPNETCORE_ENVIRONMENT=Development dotnet run
# → http://localhost:5080  (interactive API docs at /scalar/v1, health check at /health)
```

To (re-)apply migrations and seed on demand — e.g. against a fresh database — run `dotnet run -- seed`. Seeding is a no-op if the `Regions` table already has rows.

## Pulling in real data (Eurostat, no registration needed)

The app ships with a seeded placeholder dataset so the API works immediately. To replace it with real, live figures:

```bash
cd src/GermanyDashboard.Api
dotnet run -- sync-eurostat
```

Calls Eurostat's public REST API (no API key, no account) for four datasets, matched by NUTS code onto all three countries' regions plus their national totals — Germany's 16 Bundesländer are NUTS-1, Austria's 9 Bundesländer and Switzerland's 7 "Greater Regions" (Grossregionen — statistical groupings of cantons, not administrative units) are both NUTS-2:

| Indicator | Eurostat dataset | Measure |
|---|---|---|
| Population | `demo_r_pjangrp3` | Total population, 1 January |
| Life expectancy | `demo_r_mlifexp` | Life expectancy at birth |
| Unemployment rate | `lfst_r_lfu3rt` | LFS unemployment rate, ages 15–74 |
| GDP per capita | `nama_10r_2gdp` | Nominal GDP per capita (EUR) |
| Switzerland's national GDP only | `naida_10_gdp` | Total nominal GDP ÷ synced population (see gap above) |

It's a manual, explicit command — the app never calls out to Eurostat on its own — and it's safe to re-run any time to refresh the numbers (existing data points are updated in place, not duplicated).

Note the unemployment figure is the EU's harmonized LFS (ILO) definition, which can read a little differently from Germany's own registered-unemployment rate (Bundesagentur für Arbeit) — both are legitimate, differently-defined measures.

### Alternative: GENESIS-Destatis

If you'd rather source data directly from Germany's own federal statistics office instead of (or in addition to) Eurostat:

1. Register a free account at https://www-genesis.destatis.de/genesis/online.
2. Set `Destatis__Username` / `Destatis__Password` (env vars, never in source control).
3. In `src/GermanyDashboard.Api/Program.cs`, fill in the `sync-destatis` command's table-code-to-indicator-slug map, e.g. `{ "12411-0010": "population" }`.
4. Run `dotnet run -- sync-destatis`.

`GenesisApiClient` (in `GermanyDashboard.Infrastructure/ExternalServices/Destatis`) parses GENESIS's flat "ffcsv" export format; you may need to adjust column indices for the specific table you choose (documented inline).

## Tests

```bash
dotnet test
```

## Docker

```bash
docker build -f src/GermanyDashboard.Api/Dockerfile -t dachinsights-backend .
docker run -p 8080:8080 \
  -e ConnectionStrings__Default="Host=<postgres-host>;Port=5432;Database=germany_dashboard;Username=germany_dashboard;Password=..." \
  -e Cors__AllowedOrigins__0="http://localhost:3000" \
  dachinsights-backend
```

The container's `entrypoint.sh` applies migrations and seeds the demo dataset automatically (idempotent — safe on every start/restart) before starting the server. For running the full stack (Postgres + backend + frontend + Caddy) together, see the deployment repo's `docker-compose.yml` and `DEPLOY.md`.

## Security notes

CORS locked to explicit origins (GET/HEAD only — this API has no mutating endpoints), per-IP rate limiting (120 req/min), full security header set (CSP, X-Frame-Options, HSTS, etc.), global exception handler that never leaks stack traces, EF Core parameterized queries throughout, all secrets via env vars/user-secrets, dependency vulnerability scan clean (`dotnet list package --vulnerable`).
