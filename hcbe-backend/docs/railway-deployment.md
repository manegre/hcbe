# Railway production deployment

This project is prepared for a Railway Pro deployment using one GitHub monorepo and five Railway resources in one project and region:

- `frontend` with the service root directory `/hcbe-frontend`
- `api` with the service root directory `/hcbe-backend`
- `Postgres` from Railway's PostgreSQL template
- `Redis` from Railway's Redis template
- `Bucket` from Railway Storage Buckets

Use the `production` Railway environment. Create a separate Railway environment with its own database, Redis instance, bucket, and domains for staging.

## 1. Provision resources and domains

Create an empty Railway project, add `Postgres`, `Redis`, and `Bucket`, and then connect this GitHub repository to two application services. Set the API root directory to `/hcbe-backend` and the frontend root directory to `/hcbe-frontend`; Railway will then discover the Dockerfile in each directory. Keep all resources in the same region. Generate public Railway domains only for `frontend` and `api`; the database and Redis services must remain private.

The Dockerfiles listen on Railway's injected `PORT`. Configure these health checks in the service dashboard:

- `api`: `/health/ready`, 300-second timeout
- `frontend`: `/health`, 60-second timeout

Configure the API pre-deploy command as:

```text
dotnet HcbeApi.dll MigrateDatabase
```

Use `ON_FAILURE` restarts with 10 retries initially. Start with one API replica and scale only after measuring CPU, memory, database connections, and request latency.

## 2. API variables

The names below assume the Railway resources are named exactly `frontend`, `Postgres`, `Redis`, and `Bucket`. Use Railway reference-variable autocomplete rather than copying resolved secret values.

```text
ASPNETCORE_ENVIRONMENT=Production
Database__Provider=PostgreSQL
ConnectionStrings__DefaultConnection=Host=${{Postgres.PGHOST}};Port=${{Postgres.PGPORT}};Database=${{Postgres.PGDATABASE}};Username=${{Postgres.PGUSER}};Password=${{Postgres.PGPASSWORD}};SSL Mode=Prefer
ConnectionStrings__Redis=${{Redis.REDISHOST}}:${{Redis.REDISPORT}},password=${{Redis.REDISPASSWORD}},abortConnect=false
JwtSettings__Secret=<generate-and-seal-at-least-64-random-characters>
Cors__AllowedOrigins__0=https://${{frontend.RAILWAY_PUBLIC_DOMAIN}}
PublicAppUrl=https://${{frontend.RAILWAY_PUBLIC_DOMAIN}}
PublicApiUrl=https://${{RAILWAY_PUBLIC_DOMAIN}}
Authentication__Google__Enabled=true
Authentication__Google__ClientId=<google-web-client-id.apps.googleusercontent.com>
ObjectStorage__Provider=S3Compatible
ObjectStorage__ServiceUrl=${{Bucket.ENDPOINT}}
ObjectStorage__BucketName=${{Bucket.BUCKET}}
ObjectStorage__AccessKey=${{Bucket.ACCESS_KEY_ID}}
ObjectStorage__SecretKey=${{Bucket.SECRET_ACCESS_KEY}}
ObjectStorage__Region=${{Bucket.REGION}}
ObjectStorage__ForcePathStyle=false
ObjectStorage__KeyPrefix=hcbe
```

Seal the JWT value and any manually entered credentials. Railway's Postgres, Redis, and bucket references rotate with their resources without committing credentials to Git.

Production email delivery also requires:

```text
Email__Mode=Smtp
Email__Smtp__Host=<provider-host>
Email__Smtp__Port=587
Email__Smtp__EnableSsl=true
Email__Smtp__Username=<provider-username>
Email__Smtp__Password=<sealed-provider-password>
Email__FromAddress=<verified-sender-address>
Email__FromName=HCBE Canada
```

## 3. Frontend variables

Set this before building the frontend image:

```text
VITE_API_URL=https://${{api.RAILWAY_PUBLIC_DOMAIN}}
VITE_GOOGLE_CLIENT_ID=<same-google-web-client-id.apps.googleusercontent.com>
VITE_ENABLE_MEMBER_LOGIN=true
VITE_ENABLE_ADMIN_TEAM_MEMBERS=true
```

Vite embeds `VITE_*` variables at build time, so changing one requires a frontend redeploy. The Google Cloud web client must authorize the frontend's exact HTTPS origin plus `http://localhost:3000` for local development. No Google client secret is used by the browser ID-token flow.

## 4. First release

Deploy the API first. The pre-deploy command applies the committed PostgreSQL migrations before Railway activates the new container. Confirm `/health/ready`, then deploy the frontend and exercise login, uploads, document downloads, newsletter subscription, and realtime messaging.

Create the first administrator only after the API is healthy. Temporarily set sealed `HCBE_ADMIN_EMAIL` and `HCBE_ADMIN_PASSWORD` variables, then run:

```text
railway ssh --service api -- dotnet HcbeApi.dll CreateAdmin
```

Remove both bootstrap variables immediately afterward.

## 5. Domains and recovery

After custom domains are attached, replace `Cors__AllowedOrigins__0`, `PublicAppUrl`, `PublicApiUrl`, and `VITE_API_URL` with the custom HTTPS domains and redeploy both services.

Enable daily PostgreSQL volume backups and PITR. In addition, schedule an encrypted `pg_dump` to storage outside the Railway project and perform a restore drill before launch. Set Railway usage alerts and a hard spending limit high enough that an ordinary traffic spike does not take the public site offline.
