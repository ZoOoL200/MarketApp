# Standalone stakeholder expenses

Rent belongs to the stakeholder's separate expense register. It records where money was spent, like a spreadsheet, without changing any profit calculation.

| Entry | Table | Effect on profit |
|---|---|---|
| Seller salary / branch expense | Existing `BranchExpenses` | Existing branch net-profit deduction |
| Stakeholder-paid rent | New `StakeholderExpenses` | None |
| Other stakeholder spending | New `StakeholderExpenses` | None |

Each entry stores category, amount, date, description, optional payee/reference, optional branch, and creation/void audit. The optional branch describes the expense; it never charges the branch manager. Only stakeholders can use the register. Its `activeAmount` is an independent spending total.

## Apply from Visual Studio Terminal

Apply after `marketapp-profit-accounting.patch`. Stop the API and copy `marketapp-stakeholder-expenses.patch` into your solution folder. Run each command separately; stop if one fails.

```powershell
git apply --check .\marketapp-stakeholder-expenses.patch
```

```powershell
git apply .\marketapp-stakeholder-expenses.patch
```

```powershell
dotnet build MarketApp.slnx
```

```powershell
dotnet tool restore
```

Check your API connection points to `MarketApp_Test`, then:

```powershell
dotnet ef database update --project src/MarketApp.Infrastructure --startup-project src/MarketApp.Api
```

The migration `20261009155617_AddStakeholderExpenseRegister` is included. **Do not run Add-Migration for this feature.** It creates one table and its indexes, foreign keys and checks. No existing expenses are moved or deleted. The incremental SQL is in `docs/stakeholder-expenses-upgrade.sql` for review; normally use `database update`, not both installation methods.

```powershell
dotnet run --project src/MarketApp.Api -- --environment Development
```

Open `src/MarketApp.Api/ControllersTester/StakeholderExpenses.http`, fill the token/IDs/UTC dates, and send requests individually.

## If you already followed the earlier Rent instructions

Keep your existing `AddRentExpenseCategory` migration if already applied. This patch leaves those category-validation/configuration files untouched. It adds an application check to reject **new** rent records in branch expenses, even when your local validation still allows Rent. If you have not made the earlier changes, skip them: Rent now uses `/api/stakeholder-expenses`.

Existing rent in `BranchExpenses` remains there and continues to deduct from branch net profit until corrected:

1. Read the original amount, branch, expense date and description.
2. Create the equivalent stakeholder expense with a new client ID and original expense date. Include the old branch-expense ID in its description/reference for traceability. Preserve the new client ID for retries.
3. Void the old branch expense using `POST /api/branches/{branchId}/expenses/{expenseId}/void`, with a reason referencing the new stakeholder expense ID.
4. Verify both records. Voiding removes the original branch deduction; the standalone expense itself has no effect on profits.

These are two separate requests, so complete both and retain IDs to resume safely if interrupted. No automatic data movement is performed. Do not delete a previously applied migration to achieve this correction.

## API and usage

All four endpoints are stakeholder-only. Sellers/managers receive 403.

| Method | Route | Purpose |
|---|---|---|
| POST | `/api/stakeholder-expenses` | Record Rent or Other spending |
| GET | `/api/stakeholder-expenses` | Paged register and independent active spending total |
| GET | `/api/stakeholder-expenses/{expenseId}` | Read one entry including audit |
| POST | `/api/stakeholder-expenses/{expenseId}/void` | Void an incorrect entry, preserving its history |

Create request example (replace identifiers and date):

```json
{
  "clientExpenseId": "NEW-GUID-HERE",
  "branchId": null,
  "category": "Rent",
  "amount": 500,
  "occurredAtUtc": "2026-10-09T10:00:00Z",
  "description": "October premises rent",
  "paidTo": "Landlord name",
  "referenceNumber": "RENT-2026-10"
}
```

Use a branch GUID to associate the expense with a branch, or omit the field/use JSON `null` for general spending. Existing inactive branches can be referenced for historical expenses. Category values are case-sensitive `Rent` and `Other`; seller salaries continue to use the branch-expense endpoint with `Salary`. Amount must be positive with at most four decimals. Expense date must be UTC, from year 2000 through server time plus five minutes. Description is required; payee/reference are optional.

List filters: `branchId`, `category`, `fromUtc` (inclusive), `toUtc` (exclusive), `search` (description/payee/reference), `includeVoided`, `pageNumber`, `pageSize` (1–100). Omitted dates return all-time entries; periods may exceed 31 days. Search follows database text-comparison rules. Results sort by expense date descending, then ID. Voids are hidden by default. `includeVoided=true` shows them for audit, but `activeAmount` always totals only matching non-voided entries across **all pages**. It never deducts from profit.

Exact create retries by the same stakeholder return the existing record. Reusing an identifier with changed fields/user returns 409. Retrying a voided create never reactivates it. To correct amount/category/date, void the entry with a reason and create a replacement with a new client identifier. No hard-delete endpoint is provided. The API records spending; it does not execute payments.

## Acceptance example

With two units sold at 170, baseline 100, purchase cost 80, and branch salary 100, adding stakeholder rent 500 produces:

- Stakeholder profit: **40**, unchanged.
- Branch gross profit: **140**, unchanged.
- Branch net profit: **40**, unchanged.
- Standalone active spending: increases by **500**.

Compare profit requests in the same reporting period before/after adding and voiding rent. See `docs/stakeholder-expenses-verification.md` for tests and PostgreSQL acceptance limits.
