# PostgreSQL test fixes

Applies to commit `25833c922583354c0960c9f4a21f55b551f35874` on
`Test_chatgpt_finished_project`, including the stakeholder-expense reconciliation migration.

## Report and changes

The supplied API-only TRX report contains 91 tests: 88 passed and three failed.
The earlier whole-solution run contained 122 tests: 119 passed and three failed.

- Concurrent sales and returns each produced one successful response and one HTTP 500,
  while the tests expected the rejected request to return HTTP 409. The existing
  handler inspected only one inner exception. The fix inspects the complete inner
  chain for the same four PostgreSQL conflict codes; unrelated errors remain errors.
- The stakeholder-expense retry returned the same expense ID but failed whole-DTO
  equality. PostgreSQL stores microseconds; .NET DateTime can contain finer ticks.
  Expense occurrence, creation and void dates now use microsecond precision before
  persistence. The original request remains unchanged for replay hashing.
- Eight handler regression cases are added. Expense tests now force a finer-than-
  microsecond date and compare creation, retry, read and void responses.

The TRX does not contain the server exception chain or full fractional timestamp
values. These source defects are consistent with the failures; the PostgreSQL
rerun below must confirm the diagnosis. The patch does not relax the existing
concurrency assertions or bypass transaction checks.

No database migration, configuration change or data correction is required.

## Apply in Visual Studio Terminal (PowerShell)

Stop the running API. Put `marketapp-postgres-test-fixes.patch` beside `MarketApp.sln`.
Run each command separately; continue only if the previous command succeeds.

```powershell
git apply --check .\marketapp-postgres-test-fixes.patch
git apply .\marketapp-postgres-test-fixes.patch
dotnet build MarketApp.sln
```

## Verify against your test database

```powershell
$env:MARKETAPP_TEST_POSTGRES = "Host=localhost;Port=5432;Database=MarketApp_Test;Username=postgres;Password=YOUR_LOCAL_PASSWORD"
dotnet test tests/MarketApp.Api.Tests/MarketApp.Api.Tests.csproj --logger "trx;LogFileName=marketapp-postgres-after-fix.trx" --results-directory .\TestResults
Remove-Item Env:MARKETAPP_TEST_POSTGRES
```

Replace the password locally. Tests create/drop isolated schemas and do not apply
migrations. The API project now has 99 cases (91 existing plus eight new). Expected
acceptance is zero failed and zero skipped with PostgreSQL configured. This is an
expected result, not a completed verification result. Upload the new TRX if any
test fails. The current environment did not execute the patched test suite.

References:
- https://github.com/npgsql/efcore.pg/blob/main/src/EFCore.PG/Storage/Internal/NpgsqlExecutionStrategy.cs
- https://www.npgsql.org/doc/types/datetime.html
