# Profit-accounting verification — 9 October 2026

This is an incremental correction to the delivered Batch 3 files. Existing invoices and legacy percentage records are preserved; the new business rules supersede percentage shares and new damaged returns.

## Completed checks

- .NET SDK 10.0.401: solution build succeeded with zero warnings and zero errors.
- Full automated suite: **101 passed, 0 failed, 5 skipped**: 70 API, 16 application, and 15 photo tests passed.
- Both sale margins remain unchanged after current baseline updates and later differently priced purchases.
- All-branch stakeholder totals and filtering were verified with sales in two branches.
- Weighted purchase receipts, original return costs, and transfer receipt/physical-return cost snapshots were verified. Empty stock resets purchase average to the incoming receipt; unknown costs remain explicit.
- Partial/full accepted returns reverse original sale values, preserve invoices, and restock once. New damaged/non-restocked returns reject without changing stock, returns, or profit.
- Salary expenses affect branch net profit only. Tests cover request replay, conflicting retries, invalid amounts, invalid employees/dates, assigned-branch access, expense void audit, and report period boundaries.
- Existing unknown stock blocks online sales until verified initialization. Initialization checks expected quantity and cannot overwrite known cost. Missing old sale cost produces an incomplete stakeholder report; verified one-time historical cost fill is audited and cannot reprice a known cost.
- Seller and manager purchase-cost privacy was tested. Seller invoice, catalog, and stock responses do not expose baseline or purchase cost.
- Offline cost history, cost-change reconciliation, and missing historical costs were tested. Existing Batch 1/2 sale/return request shapes and hashes remain compatible.
- OpenAPI export passed its protected-route test; reference contains **82 operations, 63 paths and 85 schemas**.
- Migration `20261009091611_CorrectProfitAccountingAndBranchExpenses` generated successfully. EF reports no pending model changes. Incremental PostgreSQL upgrade SQL contains additions, not deletion/repricing of old records.
- Patch application is checked against the restored delivered Batch 3 tree. Existing appsettings, secrets, and previous HTTP request files are excluded.

## PostgreSQL acceptance on your computer

No live PostgreSQL server was available in this environment. API integration tests used real controllers/services/repositories with SQLite transactions. SQLite tests disable PostgreSQL-specific check constraints/xmin behavior, so they do not establish PostgreSQL concurrency correctness or prove that your existing data can migrate. The generated migration has not been applied to your database.

Five PostgreSQL-only tests are skipped by default: concurrent sales, concurrent returns, duplicate offline sync, JWT logout/session revocation, and refresh-token reuse. Run against your test database:

```powershell
$env:MARKETAPP_TEST_POSTGRES = "Host=localhost;Port=5432;Database=MarketApp_Test;Username=postgres;Password=YOUR_LOCAL_PASSWORD"
dotnet test tests/MarketApp.Api.Tests/MarketApp.Api.Tests.csproj
Remove-Item Env:MARKETAPP_TEST_POSTGRES
```

Tests create/drop isolated random schemas; the database account needs schema permissions. They create tables from the EF model. Applying the included incremental migration to your restored test copy remains a separate acceptance step, followed by the current `ProfitAccounting.http` requests.

Historical sale costs are not automatically reconstructable from the old database. Stock initialization and old-sale cost entry require verified records. Missing-cost results are deliberately null instead of guessed. Current reports are margin/expense reports; they do not execute payroll or payments.
