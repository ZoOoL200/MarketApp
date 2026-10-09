# Profit-accounting correction — apply after Batch 3

This patch updates the existing MarketApp API. It adds a migration and `ProfitAccounting.http`; it does not replace appsettings, credentials, or any previously delivered HTTP request file. The previous Batch 3 percentage-share feature is retired.

## 1. Apply from Visual Studio Terminal (PowerShell)

Stop the API. Open the terminal in the solution folder (the folder containing `MarketApp.slnx` and `src`). Batches 1, 2 and 3 must already be applied. If Batch 3 has not been applied, apply it first. Keep using your restored `MarketApp_Test` database for these checks. Keep your database backup and current source changes.

Copy `marketapp-profit-accounting.patch` into the solution folder. Run each command separately; stop at the first error.

```powershell
git apply --check .\marketapp-profit-accounting.patch
```

No output means the check passed. If it fails, do not force the patch or discard your changes; resolve the conflicting file/version first.

```powershell
git apply .\marketapp-profit-accounting.patch
```

```powershell
dotnet build MarketApp.slnx
```

```powershell
dotnet tool restore
```

The migration is already included: **do not run Add-Migration**. Check that the connection used by the API points to `MarketApp_Test`, then:

```powershell
dotnet ef database update --project src/MarketApp.Infrastructure --startup-project src/MarketApp.Api
```

This applies pending migrations including `20261009091611_CorrectProfitAccountingAndBranchExpenses`. The new migration adds nullable cost fields, cost history, branch expenses, keys and checks. It preserves existing invoices, returns, stock quantities, baseline prices, users, and legacy percentage records. A rollback of this migration would remove the newly collected accounting data; use your backup to recover if needed, not a casual downgrade.

```powershell
dotnet test MarketApp.slnx
```

```powershell
dotnet run --project src/MarketApp.Api -- --environment Development
```

`docs/profit-accounting-upgrade.sql` is the reviewed incremental SQL from Batch 3. Normally use `database update` above; do not separately run both workflows.

## 2. Initialize existing inventory costs before new online sales

The old API did not store purchase cost on stock balances or sales lines. Existing records therefore have `null` cost after migration. Zero is a real cost and must not be used as a substitute for unknown.

1. Log in as stakeholder and open `src/MarketApp.Api/ControllersTester/ProfitAccounting.http`.
2. Fill its URL, token, branch/product/location IDs. Run request 1 to read current stock and cost.
3. For each balance whose cost is null, verify its current weighted average purchase cost from your records. For example, 5 units at 80 plus 5 at 90 means cost 85.
4. Run request 2 with that verified cost, exact current quantity, reason, and a fresh `clientChangeId` for each product/location. Repeat across all pages/locations as needed. It returns 409 if quantity changed or cost is already known.
5. This initializes current stock only. It does not rewrite old invoices. New online sales now freeze that cost together with their baseline and actual selling price.

If existing stock has unknown cost, receiving more stock into it cannot establish the old goods' cost: the resulting average stays unknown. Initialize verified existing stock before receiving additional purchases/transfers where practical. A new empty location receives a known purchase cost automatically from a new purchase or costed transfer.

Purchase cost uses moving weighted average; baseline and minimum-price rules remain separate. **No FIFO is used.** A new/manual baseline remains the current baseline for future sales; it never changes prices saved on a completed sale.

## 3. Test the business rules

Use a test product with baseline 100, minimum price 150, known purchase cost 80 and at least two units. Fetch its current price revision. Send the new HTTP file's requests one at a time. Copy the returned sale, sale-line, and expense IDs into the variables. Use a UTC reporting range of at most 31 days containing the test transactions.

| Step | Stakeholder profit | Branch gross profit | Branch expenses | Branch net profit |
|---|---:|---:|---:|---:|
| Sell 2 at 170 | 40 | 140 | 0 | 140 |
| Record salary 100 | 40 | 140 | 100 | 40 |
| Try damaged return | 40 | 140 | 100 | 40 |
| Accept 1 undamaged return | 20 | 70 | 100 | -30 |
| Accept remaining undamaged unit | 0 | 0 | 100 | -100 |

Existing sales/expenses in the period contribute too; compare the before/after report difference. Salary is independent of returning an invoice. A full return leaves the original invoice and linked returns in history; no invoice is deleted. Every accepted new return adds stock back exactly once. `restock=false` returns 400.

Also check:

- The all-branch stakeholder report equals its branch totals; the optional `branchId` filter returns that branch only.
- An identical sale, return, or expense retry has no duplicate effects. A changed payload with the same client identifier returns 409.
- Change current baseline or receive differently priced stock after a sale; its accounting endpoint still shows the saved prices.
- Seller gets 403 for stock costs, profits, expenses, and accounting; manager gets 403 for stakeholder purchase costs and only accesses assigned branches.
- Voiding a mistaken expense retains its audit record and removes its amount from the original expense period. Use a new identifier for a replacement.

The old `Batch3.http` percentage-share requests now return 410. Its unrelated staff and health requests remain usable. Older `Batch2.http` damaged-return examples now return 400. Use `ProfitAccounting.http` for the current accounting workflow.

## 4. Complete old sale costs only from verified historical records

Stakeholder report returns `grossProfit: null`, `netPurchaseCost: null` and `missingCostEntries > 0` if a contributing old sale/return lacks its original cost. This is intentional: the API cannot infer an old sale's cost from today's purchase price.

Use the sale accounting endpoint to find missing costs. The optional last HTTP request fills an unknown sale-line purchase cost once, with the verified original value and a reason. It saves the actor/time/reason, supports exact retries, and rejects overwriting known costs. Current inventory is not revalued by this action. Branch gross/net profit remains available from the pre-existing baseline/selling snapshots.

## 5. API documentation and verification

- `docs/api/usage-guide.md`: current workflows, arithmetic, cost history, offline reconciliation, expenses, and permissions.
- `docs/api/reference.md`: endpoint/DTO reference.
- `docs/api/openapi.json`: importable specification; live version requires stakeholder token.
- `docs/profit-accounting-verification.md`: test evidence and remaining PostgreSQL acceptance checks.

Expenses are recorded manually. This update does not execute payroll, card refunds, or bank transfers.
