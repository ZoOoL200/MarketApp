> Historical batch document. After applying the profit-accounting patch, follow [the current update](profit-accounting-update.md): percentage shares are retired and new damaged returns are rejected.

# Batch 2 verification — 8 October 2026

Baseline: the exact uploaded `marketapp-batch1-sales.patch`, applied to the product-photo GitHub revision `3e744044e46663c045f3dcde3a49691fc1792bf2`.

- .NET SDK 10.0.401, EF tooling 10.0.12.
- Solution build: succeeded, zero warnings and zero errors.
- Tests: **67 passed, zero failed, 5 skipped** (13 application, 14 photo, 40 API tests passed).
- API tests use actual controllers, authorization policies, services, repositories and relational SQLite transactions. PostgreSQL-specific check constraints and row version behavior are not simulated by SQLite.
- Passed scenarios include original sales behavior, partial/restocked/damaged returns, refund/stock replay protection, over-return rejection, multi-line rollback, permissions and branch isolation, audited adjustments, stock limits, offline timestamp/history validation, stale-price reconciliation, persisted Batch 1 online request hash compatibility, historical profit and return date boundaries.
- Incremental `AddReturnsSyncAndReports` migration and PostgreSQL upgrade/downgrade SQL generation succeeded. EF reports no pending model changes. The generated upgrade SQL is supplied for review in `batch-2-upgrade.sql`; normal installation uses `dotnet ef database update`.
- Generated SQL relies on PostgreSQL's existing `xmin` system column for sales-line concurrency; it does not attempt to add a physical `xmin` column.
- The incremental patch was checked against its Batch 1 base and reconstructs the exact intended Git tree.

## PostgreSQL checks still required locally

A running PostgreSQL server was not available in the build environment. The migration has **not** been executed against a live PostgreSQL database here. Five optional PostgreSQL tests were skipped: concurrent sales, concurrent returns, duplicate offline sync, JWT logout/session revocation, and refresh-token reuse.

After applying the patch to your project, point your Development connection string at `MarketApp_Test`, run the included migration, and follow the manual HTTP acceptance checklist in `batch-2-update.md`.

To run the provider-specific automated tests too, set a connection string for a dedicated local test database in Visual Studio Terminal (replace the placeholders yourself):

```powershell
$env:MARKETAPP_TEST_POSTGRES = "Host=localhost;Port=5432;Database=MarketApp_Test;Username=postgres;Password=YOUR_LOCAL_PASSWORD"
dotnet test tests/MarketApp.Api.Tests/MarketApp.Api.Tests.csproj
Remove-Item Env:MARKETAPP_TEST_POSTGRES
```

The fixture creates a separate randomly named schema per test, seeds test records, and drops that schema on disposal. It does not apply migrations to your normal application tables; successful `database update` and your manual HTTP checks remain separate acceptance steps. The database user must be allowed to create/drop these test schemas. If a run is interrupted, a `market_test_...` schema may remain and can be inspected and removed later.

Batch 3 remains: manager profit shares, production configuration, final end-to-end verification, and full API/usage documentation. Batch 2's offline work covers server synchronization, not the future desktop application's local storage or interface.
