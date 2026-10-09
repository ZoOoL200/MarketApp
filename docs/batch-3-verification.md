> Historical batch document. After applying the profit-accounting patch, follow [the current update](profit-accounting-update.md): percentage shares are retired and new damaged returns are rejected.

# Batch 3 verification — 8 October 2026

Baseline: exact saved Batch 1 and Batch 2 patches applied to the product-photo GitHub revision `3e744044e46663c045f3dcde3a49691fc1792bf2`. This final patch targets the resulting tested Batch 2 state.

## Completed checks

- .NET SDK 10.0.401 / EF tooling 10.0.12; solution build passed with **zero warnings and zero errors**.
- Full automated suite: **93 passed, zero failed, five skipped** (13 application, 15 photo, 65 API tests passed).
- Manager tests cover percentage validation/explicit configuration, assigned-manager/stakeholder permissions, exact request replay, immutable sale snapshots, no retroactive assignment, 0% stop, offline effective-time selection, restocked/damaged refunds, and manager earnings privacy.
- Integration test covers purchase receipt → warehouse stock → shipped transfer/in-transit stock → branch receipt/baseline → sale → return → gross-profit/manager earnings, using real controllers, services, repositories and SQLite transactions.
- Staff tests cover four-character initial passwords, deactivation/reactivation/reassignment, old JWT rejection, stakeholder protection, password reset/token reuse rejection, and session invalidation after reset.
- Operational tests cover explicit production settings, CORS allow/deny behavior, authentication rate limits, protected OpenAPI, and health endpoint permissions.
- Photo tests include a complex image compressed to at most 153,600 stored bytes.
- The API reference and OpenAPI snapshot cover 74 operations and 77 schemas. After completing response metadata, the OpenAPI and health/CORS checks were rerun and passed.
- A real `Program` startup smoke check passed with Development configuration: `/health/live` returned 200; unauthenticated OpenAPI, staff-list, and readiness requests returned 401.
- Incremental `AddManagerProfitShares` migration generated successfully. EF reports no pending model changes. Upgrade and downgrade PostgreSQL scripts generated successfully. The upgrade adds one agreement table and two sales snapshot fields, with required keys/indexes/checks; existing sales default to no manager/0%.
- Final patch application is verified against the Batch 2 base and reproduces the intended Git tree. Existing appsettings and previous HTTP files are not included in the patch.

## Checks to run on your computer / hosting environment

SQLite is a relational test substitute. It does not validate PostgreSQL xmin, all provider-specific constraints, or actual PostgreSQL concurrent transactions. No live PostgreSQL database was available here, and the migration has not been executed against your database.

Five PostgreSQL-only tests remain skipped in the default run: concurrent sales, concurrent returns, duplicate offline sync, logout/session revocation, and refresh-token reuse. Run them on your dedicated test database:

```powershell
$env:MARKETAPP_TEST_POSTGRES = "Host=localhost;Port=5432;Database=MarketApp_Test;Username=postgres;Password=YOUR_LOCAL_PASSWORD"
dotnet test tests/MarketApp.Api.Tests/MarketApp.Api.Tests.csproj
Remove-Item Env:MARKETAPP_TEST_POSTGRES
```

The fixture creates a random isolated `market_test_...` schema for each test and drops it afterward; it does not use your application's existing tables. The account must have schema creation/deletion permission. Interrupted runs can leave an isolated schema for later cleanup. The tests create their schema from the model; applying the actual incremental migration to your restored database is a separate acceptance step.

Real SMTP delivery, S3 access, TLS/reverse proxy setup, host filesystem permissions, backup restoration, and deployment load limits remain environment acceptance checks. Manager reports show gross-profit participation, not a payroll settlement/payment system. There is no desktop/local queue UI or manager website in this patch.
