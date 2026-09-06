# HCBE Backend API

.NET 8 Minimal API backend for HCBE-Canada application.

## Prerequisites

- .NET 8 SDK (pour le développement local)
- PostgreSQL for production; SQLite is supported for isolated local development and tests
- Docker for production-image verification

## Setup

1. Restore dependencies:
```bash
dotnet restore
```

2. Run database migrations:
```bash
dotnet ef database update
```

This will create the SQLite database and apply all migrations.

3. Run the application:
```bash
dotnet run
```

The API will be available at `http://localhost:5000` (or check `Properties/launchSettings.json` for the configured port).

## Configuration

Edit `appsettings.json` to configure:
- Database connection string
- JWT settings (secret key, issuer, audience, expiration)
- CORS allowed origins (add your frontend domain for production)
- File upload settings

### Production Configuration

For production deployment:

1. **CORS Settings**: Update `Cors.AllowedOrigins` in `appsettings.json` to include your frontend domain:
   ```json
   "Cors": {
     "AllowedOrigins": [
       "https://your-frontend-domain.com",
       "http://localhost:5173"
     ]
   }
   ```

2. **JWT Secret**: Use a strong, randomly generated secret key in production. Never commit secrets to version control.

3. **Database**: Ensure the database file path is accessible and persistent in your deployment environment.

4. **Environment Variables**: Use environment variables or secure configuration for production secrets.

## Database Migrations

Create a new migration:
```bash
dotnet ef migrations add MigrationName
```

Apply migrations:
```bash
dotnet ef database update
```

## API Documentation

When running in development mode, Swagger UI is available at:
- `http://localhost:5000/swagger`

## Project Structure

- `Models/` - Entity models and DTOs
- `Data/` - Database context and seeding
- `Services/` - Business logic services
- `Program.cs` - Application entry point and API endpoints
- `wwwroot/uploads/` - File upload storage

## Authentication

The API uses JWT Bearer authentication. To access protected endpoints:
1. Login via `/api/auth/login`. Public self-registration is intentionally disabled; member accounts are created through the membership approval workflow.
2. Include the JWT token in the `Authorization` header: `Bearer {token}`

Admin endpoints require the user to have `isAdmin: true` in their JWT claims.

## Deployment

This backend is designed to be deployed independently from the frontend. Common deployment options:

### Railway

The deployed platform uses Railway with separate staging and production
environments. The API container is built from this directory, PostgreSQL and
Redis remain private, uploads use S3-compatible object storage, and
`dotnet HcbeApi.dll MigrateDatabase` runs as the API pre-deploy command.

See [the Railway deployment guide](docs/railway-deployment.md) and
[the release runbook](docs/release-runbook.md) for the authoritative setup,
health checks, backup procedure, and rollback process.

### Important Deployment Notes

1. **CORS**: Ensure CORS is configured to allow requests from your frontend domain
2. **Database**: production must use managed PostgreSQL; SQLite is local-only
3. **File Uploads**: production files must use the configured private object-storage bucket
4. **Environment Variables**: Use production environment variables for sensitive configuration

## Creating Admin User

To create an admin user, run:
```bash
export HCBE_ADMIN_EMAIL="admin@example.org"
export HCBE_ADMIN_PASSWORD="use-a-strong-password"
dotnet run -- CreateAdmin
```

`HCBE_ADMIN_EMAIL` and `HCBE_ADMIN_PASSWORD` are required. The password must contain at least 12 characters. No fixed production credentials are stored in the repository.

## CI/CD with GitHub Actions

This project includes GitHub Actions workflows for automated CI/CD to Azure App Service:

### Azure Deployment Workflows

1. **Automatic Deployment** (`.github/workflows/azure-deploy.yml`)
   - Runs on push to `main` branch
   - Automatically builds and deploys to Azure App Service
   - Uses publish profile authentication

2. **Manual Deployment** (`.github/workflows/azure-deploy-manual.yml`)
   - Manual trigger with environment selection (production/staging)
   - Includes test execution before deployment
   - Supports multiple environments

### Setup Instructions

#### Prerequisites

1. **Azure App Service**: Create an Azure App Service for your .NET application
2. **GitHub Secrets**: Configure the following secrets in your GitHub repository

#### Required GitHub Secrets

1. **AZURE_WEBAPP_PUBLISH_PROFILE** (Recommended method)
   - Go to your Azure App Service → Overview → Get publish profile
   - Download the `.PublishSettings` file
   - Copy the entire XML content
   - Add it as a secret named `AZURE_WEBAPP_PUBLISH_PROFILE` in GitHub Settings → Secrets

2. **AZURE_WEBAPP_NAME** (For manual workflow)
   - Your Azure App Service name (e.g., `hcbe-api`)
   - Add as a secret named `AZURE_WEBAPP_NAME`

3. **AZURE_CREDENTIALS** (Alternative authentication - optional)
   - Azure Service Principal credentials in JSON format:
     ```json
     {
       "clientId": "<client-id>",
       "clientSecret": "<client-secret>",
       "subscriptionId": "<subscription-id>",
       "tenantId": "<tenant-id>"
     }
     ```

#### Configuration Steps

1. **Update Workflow File**:
   - Edit `.github/workflows/azure-deploy.yml`
   - Update `AZURE_WEBAPP_NAME` environment variable with your App Service name

2. **Configure Azure App Service**:
   - Set the .NET version to 8.0 in App Service Configuration
   - Configure application settings (ConnectionStrings, JwtSettings, etc.) in Azure Portal
   - Set up CORS allowed origins for your frontend domain

3. **Database Configuration**:
   - For SQLite: Ensure persistent storage is configured (consider Azure Files or Blob Storage)
   - Alternatively, migrate to Azure SQL Database for production

4. **Environment Variables in Azure**:
   - Add these in Azure Portal → Configuration → Application settings:
     - `ConnectionStrings__DefaultConnection`
     - `JwtSettings__Secret`
     - `JwtSettings__Issuer`
     - `JwtSettings__Audience`
     - `Cors__AllowedOrigins` (comma-separated)

#### Deployment Process

- **Automatic**: Push to `main` branch triggers automatic deployment
- **Manual**: Go to Actions → "Deploy to Azure App Service (Manual)" → Run workflow

### Docker Support
Docker is the recommended way to run this backend in a self-contained environment. The image exposes port 8080 by default.

#### Option A — Docker Compose (recommended)

Compose configuration is provided in [hcbe-backend/docker-compose.yml](hcbe-backend/docker-compose.yml). It maps persistent volumes for SQLite DB and uploads.

```bash
cd /Users/fabrice/Dev/hcbe/hcbe-backend

# Build and start in background
docker compose up -d

# Rebuild if needed
docker compose build --no-cache
```

- Ports: host `8080` → container `8080`
- Volumes:
   - `./data` → `/app/data` (SQLite file `hcbe.db`)
   - `./wwwroot/uploads` → `/app/wwwroot/uploads` (file storage)
- Env vars in compose:
   - `ASPNETCORE_URLS=http://+:8080`
   - `ConnectionStrings__DefaultConnection=Data Source=/app/data/hcbe.db`

Health and docs:
```bash
curl -s http://localhost:8080/
# Swagger UI
# Open in browser: http://localhost:8080/swagger
```

#### Option B — Plain Docker

```bash
cd /Users/fabrice/Dev/hcbe/hcbe-backend
docker build -t hcbe-api .
mkdir -p data wwwroot/uploads
docker run -d --name hcbe-api \
   -p 8080:8080 \
   -v "$PWD/wwwroot/uploads:/app/wwwroot/uploads" \
   -v "$PWD/data:/app/data" \
   -e ConnectionStrings__DefaultConnection="Data Source=/app/data/hcbe.db" \
   hcbe-api
```

Logs:
```bash
docker compose logs -f
# or
docker logs -f hcbe-api
```

#### macOS Setup (Apple Silicon)

If `docker` is not found or Docker Desktop is not installed, use the helper script:

```bash
chmod +x /Users/fabrice/Dev/hcbe/hcbe-backend/scripts/setup-docker-macos.sh
/Users/fabrice/Dev/hcbe/hcbe-backend/scripts/setup-docker-macos.sh
```

This script will:
- Install Homebrew (if missing) and configure shell env
- Install Docker Desktop or fallback to Colima + Docker CLI
- Validate `docker` and `docker compose`

Troubleshooting:
- Ensure Docker Desktop is running (or `colima start` if using Colima)
- Restart your terminal after Homebrew install, or run:
   ```bash
   eval "$(/opt/homebrew/bin/brew shellenv)"
   ```
- If compose fails with build errors, try:
   ```bash
   docker compose build --no-cache
   docker compose up -d
   ```

#### Notes
- Predictable test administrators are seeded only in the Development environment. Create production administrators with the environment-driven `CreateAdmin` command above.
- The Dockerfile builds and publishes only the web project `[HcbeApi.csproj](hcbe-backend/HcbeApi.csproj)` for reliability.
- Update CORS allowed origins in [hcbe-backend/appsettings.json](hcbe-backend/appsettings.json) for your frontend domain.

### Railway production

The production topology, reference variables, migration command, health checks, first-admin bootstrap, and backup checklist are documented in [docs/railway-deployment.md](docs/railway-deployment.md).
