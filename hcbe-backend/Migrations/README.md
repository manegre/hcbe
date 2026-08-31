# Production migrations

This directory contains the current PostgreSQL migration lineage. It was
re-baselined from the complete application model before the production database
was introduced.

Legacy SQLite development databases are intentionally not migrated through this
lineage. They continue to use the compatibility bootstrap in `Program.cs` until
their data has been exported to PostgreSQL. New production environments must use
`Database__Provider=PostgreSQL` and apply these migrations before serving traffic.

Create future migrations with the PostgreSQL provider selected:

```powershell
$env:Database__Provider = 'PostgreSQL'
$env:ConnectionStrings__DefaultConnection = 'Host=localhost;Database=hcbe;Username=hcbe;Password=local-only'
dotnet ef migrations add <MigrationName>
```

Never edit an applied migration. Add a new migration and deploy it through the
release migration step.
