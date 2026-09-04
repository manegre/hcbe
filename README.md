# HCBE Canada platform

This repository contains the complete HCBE Canada application as a monorepo.

## Applications

| Directory | Application | Stack |
| --- | --- | --- |
| `hcbe-backend/` | API, CMS, authentication and background workers | .NET 8, Entity Framework Core |
| `hcbe-frontend/` | Public website, member area and administration UI | React 19, TypeScript, Vite |

## Local development

The frontend expects the API at `http://localhost:8080` and runs at `http://localhost:3000`.

```powershell
dotnet run --project hcbe-backend/HcbeApi.csproj --urls http://localhost:8080
npm --prefix hcbe-frontend run dev
```

Alternatively, build and run both production containers:

```powershell
docker compose up --build
```

## Verification

Install frontend dependencies once with `npm --prefix hcbe-frontend ci`, then run:

```powershell
npm run restore:backend
npm run build:backend
npm run verify
```

Run the browser suite separately with `npm run test:e2e`.

## Deployment

Railway uses this single repository for both application services. Configure the API service root directory as `/hcbe-backend` and the frontend service root directory as `/hcbe-frontend`. Each service builds its own Dockerfile.

See [the Railway deployment guide](hcbe-backend/docs/railway-deployment.md) for resources, variables, health checks, database migrations and first-administrator setup.

See [the payment and membership runbook](hcbe-backend/docs/payments-membership.md) before enabling Stripe, publishing membership plans, or accepting contributions.

## Repository history

The public monorepo starts from a clean consolidated snapshot so credentials from legacy deployment history cannot be published accidentally. The former backend and frontend histories, plus migration patches, remain available locally in the ignored `.repo-backups/` recovery directory; they are not committed or pushed.
