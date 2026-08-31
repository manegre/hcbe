# Azure App Service Deployment Guide

This guide provides detailed instructions for deploying the HCBE Backend API to Azure App Service using GitHub Actions.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Azure Setup](#azure-setup)
3. [GitHub Configuration](#github-configuration)
4. [Application Configuration](#application-configuration)
5. [Deployment](#deployment)
6. [Troubleshooting](#troubleshooting)

## Prerequisites

- Azure account with active subscription
- GitHub repository with the code
- Azure CLI (optional, for command-line setup)

## Azure Setup

### 1. Create Azure App Service

#### Using Azure Portal:

1. Navigate to [Azure Portal](https://portal.azure.com)
2. Click "Create a resource"
3. Search for "Web App" and select it
4. Fill in the details:
   - **Subscription**: Your Azure subscription
   - **Resource Group**: Create new or use existing
   - **Name**: `hcbe-api` (must be globally unique)
   - **Publish**: Code
   - **Runtime stack**: .NET 8
   - **Operating System**: Linux (recommended) or Windows
   - **Region**: Choose closest to your users
   - **App Service Plan**: Create new or use existing
5. Click "Review + create", then "Create"

#### Using Azure CLI:

```bash
# Login to Azure
az login

# Create resource group
az group create --name hcbe-rg --location eastus

# Create App Service plan
az appservice plan create \
  --name hcbe-plan \
  --resource-group hcbe-rg \
  --sku B1 \
  --is-linux

# Create Web App
az webapp create \
  --name hcbe-api \
  --resource-group hcbe-rg \
  --plan hcbe-plan \
  --runtime "DOTNET|8.0"
```

### 2. Configure App Service Settings

1. Go to your App Service in Azure Portal
2. Navigate to **Configuration** → **Application settings**
3. Add the following settings:

#### Required Settings:

```
ConnectionStrings__DefaultConnection = Data Source=/home/data/hcbe.db
JwtSettings__Secret = <your-strong-secret-key>
JwtSettings__Issuer = HcbeApi
JwtSettings__Audience = HcbeApi
JwtSettings__ExpirationInMinutes = 1440
Cors__AllowedOrigins = https://your-frontend-domain.com,http://localhost:5173
FileUpload__MaxFileSize = 10485760
```

#### Optional Settings:

```
ASPNETCORE_ENVIRONMENT = Production
ASPNETCORE_URLS = http://+:8080
```

### 3. Configure Persistent Storage (for SQLite)

Since SQLite requires persistent storage, you have two options:

#### Option A: Use Azure Files (Recommended for SQLite)

1. Create an Azure Storage Account
2. Create a File Share (e.g., `appdata`)
3. In App Service → Configuration → Path mappings:
   - Add a new path mapping:
     - **Virtual path**: `/home/data`
     - **Type**: Azure Files
     - **Storage account**: Your storage account
     - **Share name**: `appdata`

#### Option B: Migrate to Azure SQL Database (Recommended for Production)

1. Create an Azure SQL Database
2. Update connection string in App Service settings:
   ```
   ConnectionStrings__DefaultConnection = Server=tcp:your-server.database.windows.net,1433;Initial Catalog=hcbe-db;Persist Security Info=False;User ID=your-user;Password=your-password;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
   ```
3. Update `ApplicationDbContext.cs` to support both SQLite and SQL Server

### 4. Get Publish Profile

1. In Azure Portal, go to your App Service
2. Click **Get publish profile** (in the Overview section)
3. Download the `.PublishSettings` file
4. **Save this file securely** - you'll need it for GitHub Secrets

## GitHub Configuration

### 1. Add GitHub Secrets

1. Go to your GitHub repository
2. Navigate to **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret**

#### Required Secrets:

**AZURE_WEBAPP_PUBLISH_PROFILE**
- Name: `AZURE_WEBAPP_PUBLISH_PROFILE`
- Value: Open the downloaded `.PublishSettings` file and copy the entire XML content

**AZURE_WEBAPP_NAME** (for manual workflow)
- Name: `AZURE_WEBAPP_NAME`
- Value: Your App Service name (e.g., `hcbe-api`)

### 2. Update Workflow File

Edit `.github/workflows/azure-deploy.yml`:

```yaml
env:
  AZURE_WEBAPP_NAME: hcbe-api    # Update with your App Service name
```

## Application Configuration

### 1. Update CORS Settings

Before deploying, ensure your `appsettings.json` or Azure App Settings include your production frontend domain:

```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://your-production-frontend.com",
      "https://www.your-production-frontend.com"
    ]
  }
}
```

### 2. Environment-Specific Configuration

Create `appsettings.Production.json` (optional, for local testing):

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

**Note**: For Azure, use Application Settings in the portal instead of `appsettings.Production.json`.

## Deployment

### Automatic Deployment

1. Push your code to the `main` branch
2. GitHub Actions will automatically:
   - Build the application
   - Publish it
   - Deploy to Azure App Service

### Manual Deployment

1. Go to **Actions** tab in GitHub
2. Select **Deploy to Azure App Service (Manual)**
3. Click **Run workflow**
4. Select environment (production/staging)
5. Click **Run workflow**

### Verify Deployment

1. Check GitHub Actions for deployment status
2. Visit your App Service URL: `https://hcbe-api.azurewebsites.net`
3. Test the health endpoint: `https://hcbe-api.azurewebsites.net/`
4. Check Swagger (if enabled): `https://hcbe-api.azurewebsites.net/swagger`

## Post-Deployment

### 1. Run Database Migrations

If using SQLite with persistent storage:

```bash
# SSH into your App Service (if enabled)
# Or use Azure Cloud Shell
az webapp ssh --name hcbe-api --resource-group hcbe-rg

# Run migrations
dotnet ef database update
```

### 2. Create Admin User

After deployment, create the admin user:

```bash
# Using Azure Cloud Shell or SSH
cd /home/site/wwwroot
dotnet HcbeApi.dll CreateAdmin
```

### 3. Configure Custom Domain (Optional)

1. In Azure Portal → App Service → Custom domains
2. Add your custom domain
3. Configure DNS records as instructed
4. Enable SSL/TLS certificate

## Troubleshooting

### Deployment Fails

1. **Check GitHub Actions logs** for specific error messages
2. **Verify secrets** are correctly set in GitHub
3. **Check App Service name** matches in workflow file
4. **Verify publish profile** is valid and not expired

### Application Not Starting

1. **Check Application Logs**:
   - Azure Portal → App Service → Log stream
   - Or: `az webapp log tail --name hcbe-api --resource-group hcbe-rg`

2. **Common Issues**:
   - Missing application settings
   - Database connection issues
   - CORS configuration problems
   - Missing dependencies

### Database Issues

1. **SQLite File Not Found**:
   - Verify persistent storage is configured
   - Check path mappings in App Service
   - Ensure `/home/data` directory exists

2. **Permission Errors**:
   - Check file permissions on SQLite database
   - Ensure App Service has write access to data directory

### Performance Issues

1. **Enable Application Insights** for monitoring
2. **Scale up** App Service plan if needed
3. **Enable Always On** in Configuration
4. **Review** connection pooling and database queries

## Security Best Practices

1. **Never commit secrets** to version control
2. **Use Azure Key Vault** for sensitive configuration
3. **Enable HTTPS only** in App Service
4. **Configure IP restrictions** if needed
5. **Use managed identity** for Azure resource access
6. **Regularly rotate** JWT secrets and credentials
7. **Enable logging and monitoring**

## Monitoring

### Application Insights

1. Create Application Insights resource
2. Link it to your App Service
3. Monitor:
   - Request rates and response times
   - Error rates
   - Dependencies
   - Custom metrics

### Log Streaming

```bash
# Stream logs in real-time
az webapp log tail --name hcbe-api --resource-group hcbe-rg
```

## Rollback

If you need to rollback:

1. Go to Azure Portal → App Service → Deployment Center
2. View deployment history
3. Redeploy a previous version

Or use GitHub Actions to redeploy a specific commit.

## Additional Resources

- [Azure App Service Documentation](https://docs.microsoft.com/azure/app-service/)
- [.NET on Azure App Service](https://docs.microsoft.com/azure/app-service/quickstart-dotnetcore)
- [GitHub Actions for Azure](https://github.com/azure/actions)

