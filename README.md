# Planorama

Collaborative family trip planning: create a trip, invite family, suggest activities, vote, and turn the winners into a shared itinerary. Post-trip, rate events and publish the itinerary as a reusable template.

## Stack

| Piece | Tech |
|---|---|
| `backend/` | ASP.NET Core 8 Web API (C#), EF Core 8 + Npgsql, ASP.NET Identity, Hangfire worker |
| `frontend/` | Vite + React + TypeScript, PWA, CSS Modules (no CSS frameworks) |
| `shared/` | `tokens.json` design tokens, generated into `frontend/src/globals.css` |
| Infra | Docker Compose: Caddy proxy · api · worker · Postgres 16 · Redis 7 |

The frontend deploys to Cloudflare Pages; the VPS serves the API only. All business logic lives behind `/api/v1/*` — the web app is the first API client, a native app is the planned second.

## Getting started

```sh
cp .env.example .env          # fill in POSTGRES_PASSWORD + JWT_SIGNING_KEY
docker compose up             # full stack on http://localhost

# or run pieces locally:
cd backend && dotnet run --project src/Planorama.Api    # API on :5202
cd frontend && npm install && npm run dev               # web on :5173, /api proxied
```

## Development

```sh
cd backend && dotnet test                 # backend tests
cd backend && dotnet ef migrations add X --project src/Planorama.Core --startup-project src/Planorama.Api --output-dir Data/Migrations
cd frontend && npm run tokens             # regenerate globals.css from shared/tokens.json
cd frontend && npm run build              # Cloudflare Pages artifact
```

Health endpoints: `GET /healthz` (liveness), `GET /readyz` (DB reachable).
