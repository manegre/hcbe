# Testing and capacity validation

Backend CI compiles with warnings as errors, runs unit/integration tests with coverage collection, validates idempotent PostgreSQL migrations, and builds the production container. Frontend CI installs the lockfile, type-checks, blocks high/critical npm advisories, and creates the production bundle.

Run the baseline public API load profile against staging:

```bash
docker run --rm -i -e BASE_URL=https://YOUR-STAGING-APP.fly.dev grafana/k6 run - < load-tests/public-api.js
```

The profile ramps to 100 concurrent virtual users, requires fewer than 1% failed requests, and sets p95/p99 latency budgets. Run it after representative staging data is loaded and before major releases. Do not point load tests at production without an approved window.

Before a 1,000-user launch, add authenticated scenarios for login/refresh, CMS reads, messaging sends, SignalR reconnects, uploads, and newsletter queuing. Capacity acceptance should be based on measured peak concurrency rather than registered-user count. Record API/DB/Redis CPU, memory, connections, queue depth, latency, and error rate during each run.
