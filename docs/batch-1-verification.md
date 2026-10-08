# Batch 1 verification

Verified on 8 October 2026 with .NET SDK 10.0.401, EF Core 10.0.12, and Npgsql EF provider 10.0.3. Baseline: GitHub `3e74404`.

## Completed here

| Check | Result |
|---|---|
| Solution build | Passed, 0 warnings and 0 errors |
| Existing pricing tests | 13 passed |
| Existing photo tests | 14 passed |
| New API integration tests | 18 passed, 3 PostgreSQL-only tests skipped |
| EF pending-model-change check | Passed; model matches AddSalesInvoices |
| PostgreSQL upgrade SQL generation | Passed; photo migration to AddSalesInvoices |
| Actual API startup | Passed with temporary test JWT configuration |
| Anonymous request to actual sales catalog | 401, as expected |

**Total: 45 tests passed, 0 failed, 3 skipped.** The final solution test run rebuilt the API and all test projects after the migration was added.

The API tests execute real controllers, application services, EF repositories, Unit of Work, and branch authorization. They cover stock deduction, exact sell-out, invalid quantity/price, whole-invoice failure, repeated client IDs, historical price and name preservation, stale revisions, inactive/wrong-branch locations, missing pricing, seller/manager invoice visibility, pagination, and real JWT login and sale submission.

The default database for API tests is SQLite. The fixture removes PostgreSQL-specific constraints and xmin concurrency behavior for that provider. Passing these tests does not verify PostgreSQL locking, PostgreSQL constraints, migration execution, refresh-token SQL, or production deployment.

## PostgreSQL checks to run together

No running PostgreSQL test server was available here. The migration has been generated and reviewed but **has not been applied to your database or a live PostgreSQL instance here**. Test the upgrade on a copy of your current database first, then use Sales.http.

For provider-specific automated tests, create a separate empty database, for example `MarketApp_ApiTests`, and run from the solution folder:

```powershell
$env:MARKETAPP_TEST_POSTGRES = "Host=localhost;Port=5432;Database=MarketApp_ApiTests;Username=postgres;Password=YOUR_TEST_PASSWORD"
dotnet test tests/MarketApp.Api.Tests/MarketApp.Api.Tests.csproj
Remove-Item Env:MARKETAPP_TEST_POSTGRES
```

The test fixture creates and deletes its own uniquely named schema for each test. The database user needs schema creation permission. Do not point this setting at your working database. On this route every API test uses the real PostgreSQL model and constraints. These tests create tables from the model; they do not replace the separate migration upgrade test.

Three tests are enabled only with this setting:

- Two sales of seven units against ten available units: one succeeds, one conflicts, and stock ends at three.
- Login followed by logout invalidates the existing JWT session.
- Reusing a consumed refresh token revokes the replacement session.

Inspect all results; do not count skipped tests as passed. The manual checklist is in `batch-1-sales.md`. Full production configuration and final project-wide acceptance remain in Batch 3.
