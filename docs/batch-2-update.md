> Historical batch document. After applying the profit-accounting patch, follow [the current update](profit-accounting-update.md): percentage shares are retired and new damaged returns are rejected.

# Batch 2 — returns, offline sale synchronization, adjustments, and branch gross profit

This is an incremental update to your tested Batch 1. Apply the supplied patch from Visual Studio **Terminal (PowerShell)**, in the existing folder containing `MarketApp.sln`. No new application is needed. Keep using your restored `MarketApp_Test` database while testing.

## Apply the update

Stop the API/debugger and save your files. Keep a backup or local commit of your current project and test database. Put `marketapp-batch2-update.patch` beside `MarketApp.sln`.

Run each command separately; continue only if the previous command succeeds:

```powershell
git apply --check --ignore-space-change .\marketapp-batch2-update.patch
git apply --ignore-space-change .\marketapp-batch2-update.patch
dotnet build MarketApp.sln
dotnet tool restore
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet ef database update --project src/MarketApp.Infrastructure --startup-project src/MarketApp.Api
dotnet test MarketApp.sln --no-build
```

The patch already contains `AddReturnsSyncAndReports`; **do not run Add-Migration**. The migration adds three tables, return quantities, offline/reconciliation audit fields, and stock movement links/constraints. Existing invoices start with `ReturnedQuantity = 0`, `IsOffline = false`, and no reconciliation actor/time. Existing request hashes, prices, and invoice totals stay intact.

If `git apply --check` fails, stop and share its output; do not force it or use `--reject`. If you believe it was already applied, `git apply --reverse --check --ignore-space-change .\marketapp-batch2-update.patch` verifies that without changing files. Avoid rolling the database back after new returns or adjustments have been recorded; the Down migration removes those records.

Start the API normally with Visual Studio. Open `src/MarketApp.Api/ControllersTester/Batch2.http`, fill its variables, and send requests individually. Your existing configuration and HTTP files are not replaced.

## Routes and permissions

| Method and route | Access | Result |
|---|---|---|
| POST `/api/branches/{branchId}/sales/{saleId}/returns` | Stakeholder; assigned branch manager | 200 return/refund record, including replay |
| GET `/api/branches/{branchId}/sales-returns` | Stakeholder; assigned branch manager | Paged return history |
| POST `/api/stock-adjustments` | Stakeholder | 200 audited adjustment, including replay |
| GET `/api/stock-adjustments` | Stakeholder | Paged adjustment history |
| POST `/api/branches/{branchId}/sales/sync` | Stakeholder; assigned seller/manager | 201 new invoice; 200 exact replay |
| POST `/api/branches/{branchId}/sales/reconcile` | Stakeholder | 201 reconciled invoice; 200 exact replay |
| GET `/api/branches/{branchId}/reports/profit?fromUtc=...&toUtc=...` | Stakeholder; assigned branch manager | Gross branch profit |

The Batch 1 online `POST .../sales` contract stays the same: the server records the current time. Seller invoice responses now include returned quantities and offline/reconciliation flags, but never expose baseline prices or profit. Lists support `pageNumber` (default 1) and `pageSize` (default 25; max 100). Search is not implemented for return/adjustment lists.

## Returns

Supply a new `clientReturnId`, a reason, and original **sales invoice line IDs** (not product IDs). Partial returns are supported; cumulative returned quantity cannot exceed the original sold quantity. The refund always uses the original actual selling price, regardless of today's prices.

- `restock: true`: confirms the goods physically arrived back at the original branch stock location. Quantity is restored once and a linked stock movement is recorded.
- `restock: false`: records a refund for damaged/unusable goods without restoring available stock. Its original baseline cost remains in the report.

Only one line per original invoice line is accepted within a return. Quantities use at most three decimal places. The original branch/location must be active. All lines commit together or none do. Invoice original quantities/totals remain historical; `returnedQuantity` is separate. This API records the refund amount; it does not execute a bank/card refund.

## Adjustments

A stakeholder supplies `clientAdjustmentId`, location, product, signed `quantityChange`, and reason. Positive adds stock; negative removes it. Zero, excessive decimal precision, or a negative resulting stock balance is rejected. Each accepted adjustment stores actor, timestamp, reason, and a stock movement. Use this for a verified physical count/damage correction, not as a substitute for purchases, transfers, or returns.

## Offline synchronization and reconciliation

This batch implements the **server API** for accepting queued sales. A persistent local queue, local stock/cache, and device UI belong to the future Windows seller app; this patch does not create that desktop app.

At the time of an offline sale, the future client must save the complete submission durably:

```json
{
  "soldAtUtc": "ACTUAL_UTC_TIME_ENDING_Z",
  "sale": {
    "clientSaleId": "A_NEW_GUID_SAVED_WITH_THIS_SALE",
    "inventoryLocationId": "LOCATION_GUID",
    "notes": "Optional note",
    "lines": [{ "productId": "PRODUCT_GUID", "quantity": 1, "sellingUnitPrice": 170, "priceRevision": 1 }]
  }
}
```

On reconnect, submit one queued sale at a time as the original seller. Preserve the client ID, timestamp, line order, quantities, notes, and prices on every retry. This version accepts new offline submissions from the last 30 days, with at most five minutes of future clock skew; use UTC `Z`. Successful identical replays are looked up before this age check, so an acknowledged sale does not become invalid merely because it is retried later.

- 201: newly accepted; record the server invoice ID and mark the queue item posted.
- 200: already accepted with the same data; likewise mark posted.
- Connection failure/unknown response: retain the original payload and retry it, never generate another ID.
- 400: invalid submission; retain it for review instead of silently discarding it.
- 409: conflict; keep the queue item unchanged and inspect the message. A concurrency conflict may be retried unchanged. A price or stock conflict needs stakeholder review.

Normal sync requires a current price revision that was effective at the reported sale time. After a price change, an older queued sale stays blocked until the stakeholder submits `/sales/reconcile` with `originalSellerUserId` and the **identical** original payload inside `submission`. The original seller must still be active, assigned to the branch, and have Seller or BranchManager role.

Reconciliation permits an old price revision only when it was effective at that sale's time. It still enforces its historical minimum price, available stock, active branch/location/product, and the submission age limit. It records both the original seller and stakeholder/time of reconciliation. It does not silently replace sale prices, invent stock, or accept sales below the historical minimum. A below-minimum sale, unavailable stock, or missing/inconsistent price history needs investigation; this batch does not override those conditions.

The stakeholder's existing procedure remains: contact branches to stop selling for a pricing update until they reconnect. Server stock cannot reflect an offline sale until it syncs; branch operations must account for pending local sales before transferring or adjusting that stock.

## Gross profit report

Supply UTC `fromUtc` and `toUtc`, ending `Z`. Start is inclusive, end exclusive; maximum range is 31 days. Offline sales count on `soldAtUtc`, so later synchronization can update a past report. Returns count on their server posting date, even if the original sale is outside the report period.

| Field | Meaning |
|---|---|
| `saleCount` | Invoices sold in the interval, including later returns |
| `salesRevenue` | Original quantities × actual selling price |
| `refunds` | Return quantities × original actual selling price, posted in the interval |
| `netRevenue` | Sales revenue minus refunds |
| `netBaselineValue` | Original baseline snapshots of sales, minus original baseline value of restocked returns |
| `grossProfit` | Net revenue minus net baseline value |

Example: sell 2 units at 170 with baseline 100. Initial gross profit = 140. Return 1 at refund 170: restocked → gross profit 70; not restocked → gross profit -30. A return in a later period may make that period negative. These figures are branch gross sale profit, using stakeholder-to-branch baseline, not supplier cost. They exclude operating expenses, valuation of stock adjustments, and manager profit shares (Batch 3). Current manual price changes never recalculate old sale snapshots. No FIFO is introduced.

## Manual acceptance checklist

1. Run your existing `Sales.http` tests again.
2. Record starting stock; return one sold item with `restock=true`. Refund equals its original actual selling price; stock rises once. Repeat the exact request; no second effect.
3. Request more than the remaining sold quantity: 409; no new return or stock movement.
4. On another returnable item, use `restock=false`: refund recorded; stock unchanged.
5. Stakeholder adjustment +3: stock rises 3 once; repeat unchanged. A negative adjustment below zero fails. Manager and seller calls return 403.
6. Sync a recent offline sale at a current effective revision: 201, then 200 on exact retry; stock deducted once and `isOffline=true`.
7. For stale-price testing, save a **new, unsent** offline payload with the current revision/time; then change branch pricing. Submit the saved payload as seller: 409. Reconcile its exact submission as stakeholder: 201; historical prices retained. Seller retry of the same payload: 200.
8. Verify report arithmetic for the sale/return interval; seller report/return-history requests must return 403. Assigned managers cannot access another branch.

For new test operations generate fresh IDs with `[guid]::NewGuid()` in PowerShell. Generate UTC time with `(Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ")`. Keep the original saved ID/time for replays.
