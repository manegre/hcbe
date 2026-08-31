# Production database operations

Production uses managed PostgreSQL. SQLite remains a local compatibility mode
only and must not be used by a production deployment.

## Required configuration

Store the connection string as a deployment secret, never in source control:

```text
Database__Provider=PostgreSQL
ConnectionStrings__DefaultConnection=Host=...;Port=5432;Database=...;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=false
```

Fly runs `dotnet HcbeApi.dll MigrateDatabase` as a release command before the
new application version receives traffic. A failed migration blocks the
release. The web process does not mutate the schema at startup.

## Backup policy

- Enable provider-managed point-in-time recovery with at least 7 days of history.
- Keep one daily logical backup for 30 days in a separate storage account.
- Encrypt backups and restrict restore permissions to production operators.
- Record the backup timestamp, database version, and migration identifier.

Example logical backup:

```powershell
pg_dump --format=custom --no-owner --no-acl --file hcbe-production.dump $env:HCBE_DATABASE_URL
```

## Restore drill

Run this against an isolated recovery database, never the active production
database:

```powershell
createdb hcbe_recovery
pg_restore --clean --if-exists --no-owner --no-acl --dbname hcbe_recovery hcbe-production.dump
```

After restoration:

1. Run the application readiness check against the recovery database.
2. Verify administrator login, member counts, published content, and recent messages.
3. Compare row counts for critical tables with the backup manifest.
4. Record recovery-point and recovery-time results.
5. Delete the isolated recovery database after approval.

Perform a restore drill before launch and at least quarterly thereafter.
