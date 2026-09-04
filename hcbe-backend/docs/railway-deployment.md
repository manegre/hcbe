# Railway production deployment

This project is prepared for a Railway Pro deployment using one GitHub monorepo and production resources in one project and region:

- `frontend` with the service root directory `/hcbe-frontend`
- `api` with the service root directory `/hcbe-backend`
- `Postgres` from Railway's PostgreSQL template
- `Redis` from Railway's Redis template
- `Bucket` from Railway Storage Buckets
- `postgres-backup`, a private daily Railway cron service rooted at `/`, configured in `/.railway/railway.ts` to build `/ops/postgres-backup/Dockerfile`

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
DataProtection__KeyEncryptionKeys=<base64-encoded-32-byte-key>
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
Email__Mode=BrevoApi
Email__Brevo__ApiKey=<sealed-brevo-api-key>
Email__FromAddress=noreply@hcbe.ca
Email__FromName=HCBE Canada
Email__ReplyToAddress=contact@hcbe.ca
Operations__AlertEmail=<operations-team-address>
```

Payments, memberships, recurring contributions, receipts, refunds, and reconciliation require:

```text
Finance__Enabled=true
Finance__Provider=Stripe
Finance__SecretKey=<sealed-restricted-stripe-key>
Finance__WebhookSecret=<sealed-whsec-signing-secret>
Finance__AutomaticTaxEnabled=false
Finance__MembershipGracePeriodDays=30
Finance__MinimumDonationCents=500
Finance__Currency=cad
```

Create the Stripe webhook at `https://api.hcbe.ca/api/finance/webhooks/stripe` and configure the Customer Portal before enabling payments. See [payments-membership.md](payments-membership.md) for the exact webhook events, first transaction/refund check, tax guardrails, and secret rotation procedure.

`DataProtection__KeyEncryptionKeys` protects the ASP.NET Data Protection key ring. In production the key ring is shared through the private Redis service, so restarts and multiple API replicas keep cookies and protected payloads valid. Generate this value with a cryptographically secure random source and seal it in Railway. For rotation, prepend the new Base64 key and retain the old one after a comma until every Data Protection key encrypted with it has expired.

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

The `/ops/postgres-backup` Railway cron service creates a daily custom-format dump over Railway's private network, restores it into an isolated PostgreSQL 18 process matching production, validates the migration history and core tables, encrypts it with AES-256, and retains only encrypted objects for 30 days. It must reference `Postgres.DATABASE_URL`, use a long random `BACKUP_ENCRYPTION_KEY`, and receive private bucket credentials through `S3_ENDPOINT`, `S3_BUCKET`, `AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`, and `AWS_DEFAULT_REGION`. It must not have a public domain. Railway PITR can be added as a separate recovery layer after its plan and storage cost are approved and a PITR restore drill is completed.

`production-monitor.yml` checks the frontend plus API liveness and readiness every ten minutes. A failure opens or updates one GitHub issue and a recovery closes it. Subscribe the operations team to repository issue notifications. Application exceptions are also persisted in `ErrorIncidents`, shown under **Administration → Surveillance**, written as structured container logs, and emailed to `Operations__AlertEmail` through the reliable outbox.

## 6. Restore procedure

1. Download the three matching timestamped objects from the backup bucket and verify the encrypted dump with `sha256sum --check hcbe-*.dump.gpg.sha256`.
2. Decrypt it with `gpg --batch --decrypt --output hcbe.dump hcbe-*.dump.gpg`; retrieve the passphrase from the protected `BACKUP_ENCRYPTION_KEY` secret owner.
3. Provision an empty PostgreSQL database with the same or newer major version than production.
4. Restore with `pg_restore --exit-on-error --no-owner --no-acl --dbname="$TARGET_DATABASE_URL" hcbe.dump`.
5. Verify `SELECT COUNT(*) FROM "__EFMigrationsHistory";` and confirm the `Users`, `Members`, and `Events` tables exist.
6. Point a staging API at the restored database, check `/health/ready`, then test admin login and one read-only public flow before any production cutover.

Never test restoration over the production database. Quarterly, perform this procedure in an isolated Railway environment in addition to the automated daily container restore.

Last verified production drill: 2026-09-03. That isolated PostgreSQL 18 restore validated 12 EF Core migrations plus the `Users`, `Members`, and `Events` tables, and the private bucket received the encrypted dump, SHA-256 checksum, and restore-verification report. Run a new isolated restore drill after deploying `AddCommunityFinance` and verify the five finance tables before considering this release recovery-tested.
