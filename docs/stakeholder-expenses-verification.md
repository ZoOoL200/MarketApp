# Stakeholder expense verification — 9 October 2026

Baseline: the delivered `marketapp-profit-accounting.patch`, after Batches 1–3.

- Solution built with .NET SDK 10.0.401: zero warnings and zero errors.
- Full automated suite: **117 passed, 0 failed, 5 skipped** (86 API, 16 application, 15 photo tests).
- Creating and voiding standalone rent leaves branch gross/net profit, branch expenses, stakeholder profit, stock and saved purchase costs unchanged. Existing seller salary deductions remain intact.
- Tests cover exact retries, conflicting payloads/client IDs, void audit and retries, general expenses without a branch, historical inactive-branch expenses, missing references, invalid amounts/categories/dates, stakeholder-only permissions, filtered totals across pages, text search, and reporting date boundaries.
- Compatibility was checked with the earlier local Rent changes in branch DTO validation, database configuration and EF snapshot. The patch applied successfully, and two targeted API tests passed: branch rent rejection and independent stakeholder rent accounting.
- Neither profit query references StakeholderExpenses. Existing category/configuration files and previous HTTP files are excluded from this patch.
- EF reports no pending model changes. Migration `20261009155617_AddStakeholderExpenseRegister` and its incremental SQL create only the new table and its keys/indexes/checks. Existing expenses are not automatically moved or deleted.
- OpenAPI and the reference document cover **86 operations, 66 paths and 89 schemas**.

Five PostgreSQL-only tests remain skipped by default: concurrent sales, concurrent returns, duplicate offline sync, JWT logout/session revocation, and refresh-token reuse. The default API tests use real controllers/services/repositories with SQLite transactions and disable PostgreSQL-specific constraints/xmin behavior. No live PostgreSQL server or your database was available here; the migration has not been applied to your database.

Apply the migration to your restored `MarketApp_Test` database, then run `StakeholderExpenses.http`. PostgreSQL integration-test instructions are in `docs/profit-accounting-verification.md`; those tests create isolated schemas from the model, so applying the actual migration remains a separate acceptance step.
