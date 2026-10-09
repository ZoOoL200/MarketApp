# MarketApp API usage guide

This guide describes the server API after Batches 1–3. The Windows seller app and manager/stakeholder web app will call these endpoints. Their screens, offline database, and local photo cache are separate client work. The API requires an internet/network connection; offline selling occurs in the future desktop client and is uploaded later.

Use your Development HTTPS address (normally `https://localhost:7126`) while testing. Paths below are relative to that address. Send JSON using `Content-Type: application/json`, except photo uploads which use multipart form data. GUID values and tokens in examples are placeholders.

## Roles and access

| Role | Main capabilities |
|---|---|
| Stakeholder | Master data, purchases, transfers, all branch pricing, staff, stock adjustments, reconciliation, manager agreements, branch reports, photos, API documentation. |
| BranchManager | Assigned-branch sales/catalog, management prices, minimum selling price, returns, and branch gross/net profit reports and expenses. |
| Seller | Assigned-branch selling prices/catalog/stock, own sales/history, offline sync, and shared product photos. No baseline prices, profit, returns, or staff management. |

A manager/seller must be assigned to an active branch. Stakeholders can inspect inactive branches, but stock/sale writes still require active entities. Most general management endpoints rely on the application's stakeholder-only default/fallback policy. There is no public customer registration.

## 1. Login, tokens, and password recovery

Log in with a username:

```http
POST /api/auth/login
Content-Type: application/json

{ "userName": "YOUR_USERNAME", "password": "YOUR_PASSWORD" }
```

The response contains an access token, refresh token, expiry information (see OpenAPI for exact fields). For subsequent protected requests send:

```http
Authorization: Bearer YOUR_ACCESS_TOKEN
```

`GET /api/auth/me` returns the current user/session/roles. Both desktop and web clients use this JWT flow. Access tokens last the configured 1–60 minutes (default 30); sessions last seven days. `POST /api/auth/refresh` accepts `{ "refreshToken": "SAVED_REFRESH_TOKEN" }` and rotates the refresh token. Store the new token immediately and stop using the old one. Reusing an old refresh token revokes that session. A lost refresh response should lead to a fresh login rather than repeatedly replaying the old refresh token. `POST /api/auth/logout` revokes the authenticated session.

Staff can use passwords of 4–1024 characters, without mandatory digit/case/symbol requirements. Account lockout and IP rate limits still apply. Do not share individual staff accounts.

Email verification:

1. Authenticate and call `POST /api/auth/request-email-verification` with no body.
2. Read the message containing user ID and encoded token.
3. Call `POST /api/auth/confirm-email` with `{ "userId": "GUID", "token": "ENCODED_TOKEN" }`.

Forgotten password:

1. Call `POST /api/auth/forgot-password` with `{ "userName": "USERNAME" }`.
2. The generic response does not disclose whether the account exists. Only active users with a verified email are eligible.
3. Read the reset message and call `POST /api/auth/reset-password` with `userId`, `token`, `newPassword`, and matching `confirmPassword`.
4. Log in again. Old sessions are invalid after a successful reset. Tokens expire after one hour.

Development messages are saved under the current user's LocalApplicationData/MarketApp/DevelopmentMail. Production uses SMTP. The API supplies tokens; user-friendly reset/verification screens belong to the future clients.

## 2. Set up master data and staff

As stakeholder, create records in this order:

1. Category: `POST /api/categories`.
2. Product: `POST /api/products` with `name`, unique `sku`, `categoryId`, and `isActive`. A product itself does not contain one global stock quantity; quantities belong to inventory locations.
3. Branch: `POST /api/branches`.
4. Inventory location: `POST /api/inventory-locations`. Set its `branchId` for branch stock, or null for central warehouse stock.
5. Supplier: `POST /api/suppliers`.
6. Staff: `POST /api/users` with username, full name, email, initial password, role `Seller` or `BranchManager`, and active branch IDs.

Use GET/list endpoints to retrieve IDs; use the update/delete methods listed in the reference. Deletion can be blocked when records are referenced by historical operations. Prefer deactivating master records that need to remain in historical data. Your existing HTTP files contain the original workflows; do not copy real credentials into shared files.

`GET /api/users` and `GET /api/users/{id}` return staff details without password hashes or session secrets. `PUT /api/users/{id}/access` accepts:

```json
{ "isActive": true, "branchIds": ["BRANCH_GUID"] }
```

This replaces branch assignments, updates activity, and revokes that user's sessions. Active staff need at least one active branch. Deactivation can use an empty branch list. The endpoint cannot change stakeholder access, delete users, or change staff roles. Reactivated users log in again.

## 3. Receive purchased stock

Create a draft purchase invoice:

```json
{
  "supplierId": "SUPPLIER_GUID",
  "inventoryLocationId": "WAREHOUSE_GUID",
  "invoiceDate": "2026-10-08",
  "supplierInvoiceNumber": "SUP-EXAMPLE-001",
  "notes": "Example receipt",
  "lines": [{ "productId": "PRODUCT_GUID", "quantity": 10, "unitCost": 80 }]
}
```

Send it to `POST /api/purchase-invoices`. Creation alone does not add stock. Call `POST /api/purchase-invoices/{id}/post` when the goods are received; then inspect the invoice and stock movements. Posted documents are historical, not editable sales-price sources. Supplier unit cost is distinct from the stakeholder-to-branch baseline used for branch profit.

Purchase and transfer creation do not have the sales-style client ID replay contract. Save their returned server IDs; after an uncertain create response inspect the paged history before creating another document. State-changing post/ship/receive routes enforce their document state and must not be treated as freely repeatable writes.

## 4. Transfer stock between locations

`POST /api/stock-transfers` creates a draft with `sourceLocationId`, `destinationLocationId`, notes, and lines containing product ID, quantity, and branch `baselineUnitPrice`.

- Draft: stock remains at source. Canceling a draft does not move stock.
- Ship: `POST /api/stock-transfers/{id}/ship` deducts source stock; goods are InTransit and unavailable at either location.
- Receive: `POST /api/stock-transfers/{id}/receive` adds destination stock and applies eligible branch baseline updates.
- After shipping, do not restore source stock just because delivery was canceled. Request return using `/request-return` with a reason. Only `/confirm-return`, after goods physically arrive back, restores source stock.

A newer manual baseline change is protected from an older transfer receipt. Prices use revisions/history, not FIFO. Inspect `GET /api/stock-transfers/{id}` and its lines/state before each action. Partial-transfer receiving/returning is not exposed by these endpoints.

## 5. Configure branch prices

Management price: `GET /api/branches/{branchId}/product-prices/{productId}`. Stakeholders set baseline through `PUT .../{productId}/baseline`. Stakeholders or assigned branch managers set minimum selling price through `PUT .../{productId}/minimum-selling-price`.

Send `{ "price": 150, "expectedRevision": CURRENT_REVISION, "reason": "Reason for change" }`; the price is an example. Use the current revision from the management price response, or 0 for initial creation when no price record exists. A revision conflict means re-read and review the latest values. Price history is available through the management history route. You may manually change baseline during inflation without receiving another shipment.

The manager's minimum is also the recommended selling price. Sellers may charge more, never less. Sellers obtain only selling-price/catalog DTOs; supplier costs and baseline/profit fields are not exposed. Posted sales retain their original baseline, minimum, actual price, and revision snapshots. Updating current prices never rewrites earlier sale profit.

Before changing prices, follow your branch procedure: contact branches to stop selling until they reconnect and update their catalog. The API does not automatically notify offline devices.

## 6. Sell and synchronize

For a new online sale, use `POST /api/branches/{branchId}/sales`:

```json
{
  "clientSaleId": "NEW_SAVED_GUID",
  "inventoryLocationId": "BRANCH_LOCATION_GUID",
  "notes": "Optional note",
  "lines": [{ "productId": "PRODUCT_GUID", "quantity": 2, "sellingUnitPrice": 170, "priceRevision": 1 }]
}
```

Read the current catalog revision first. Each product appears once per invoice. Sale quantities use at most three decimal places; prices use at most four. All lines commit atomically. Selling exactly all available stock is valid and leaves zero; overselling is rejected.

201 means newly posted. 200 means the same authenticated seller, client ID, and identical payload was already posted. Changed data under the same ID produces 409. Keep the same ID, line order, notes, and values when retrying an uncertain response. The online server supplies sale time. Sellers can view only their own invoices; assigned managers/stakeholders can view branch history.

For a sale that already happened offline, keep its original UTC time and payload in the future desktop app's durable local queue and submit:

```json
{
  "soldAtUtc": "ACTUAL_UTC_TIME_ENDING_Z",
  "sale": {
    "clientSaleId": "ORIGINAL_SAVED_GUID",
    "inventoryLocationId": "BRANCH_LOCATION_GUID",
    "notes": "Original note",
    "lines": [{ "productId": "PRODUCT_GUID", "quantity": 2, "sellingUnitPrice": 170, "priceRevision": 1 }]
  }
}
```

Send to `POST .../sales/sync` as the original seller. New submissions must be within 30 days and no more than five minutes ahead. The price revision must have been effective at that time. Normal sync also requires the revision still be current. Preserve the queue item on errors; never silently replace its prices or invent another sale ID.

If current pricing changed, stakeholder reviews the exact payload and sends `POST .../sales/reconcile` with `{ "originalSellerUserId": "GUID", "submission": ORIGINAL_SYNC_PAYLOAD }`. Reconciliation still checks historical minimum, effective time, active entities, stock, and age. It stores the original seller plus the reconciling actor/time. It does not permit below-minimum sales or negative stock. The seller can later retry the same payload and get the same invoice.

A client must distinguish a retryable concurrency conflict from a business conflict needing review. HTTP 409 does not mean it is safe to change a completed offline sale. Sync acknowledgment determines when to mark a queue record uploaded; network failures must leave it queued. The server cannot know pending offline sales until they arrive.

## 7. Returns and stock corrections

Assigned branch manager/stakeholder: `POST .../sales/{saleId}/returns`, with `clientReturnId`, reason, and lines containing original **sales invoice line ID**, quantity, and `restock`.

Refund = returned quantity × original actual selling price. Cumulative returns cannot exceed original sold quantity. `restock=true` confirms physical receipt at the original stock location and adds stock. `restock=false` is rejected: damaged/unusable goods are not accepted. Every new accepted return must be physically received and restored to stock. Repeating an identical request returns the same record and does not refund/restock twice. No bank/card payment is executed by this API.

Stakeholder: `POST /api/stock-adjustments`, with `clientAdjustmentId`, product/location IDs, signed `quantityChange`, and reason. Positive adds stock; negative removes it. Zero, invalid precision, or a negative resulting balance is rejected. Each adjustment records actor/time/reason and a linked movement. This is for verified physical counts/damage, not an alternative to posting normal purchases or transfers.

Read return/adjustment history and stock movements to investigate differences. Server stock needs reconciliation with pending offline transactions before performing physical-count adjustments.

## 8. Profit reports, fixed costs and branch expenses

Profit is calculated from **saved sale-line prices**. Later baseline updates, purchases, or selling-price updates do not rewrite posted invoices.

| Report | Formula, per sold unit |
|---|---|
| Stakeholder gross profit | Saved baseline − saved purchase cost |
| Branch/manager gross profit | Saved actual selling price − saved baseline |
| Branch net profit | Branch gross profit − active branch expenses |

Multiply each line's margin by its quantity. Accepted returns reverse the original line's values for the returned quantity. Full returns reverse the complete sale margin while retaining invoices and linked return records. No percentage manager agreement is used. Previous percentage endpoints now return 410 and stored legacy agreements are retained for audit only.

`GET /api/reports/stakeholder-profit?fromUtc=...&toUtc=...` returns all branches and a combined total. Supply `branchId` for one branch. Only the stakeholder can read purchase costs or this report. `GET /api/branches/{branchId}/reports/profit?fromUtc=...&toUtc=...` gives `grossProfit`, `expensesTotal`, and `netProfit`; assigned managers and the stakeholder can access it.

Dates must be UTC (`Z`), start-inclusive/end-exclusive, at most 31 days. Sales belong to their original sale date; returns belong to their posting date; expenses belong to `occurredAtUtc`. A later-period return can produce negative profit in that period. Late offline synchronization can update an earlier period.

Example: purchase cost 80, baseline 100, actual sale price 170, quantity 2. Stakeholder profit = 40; branch gross profit = 140. A seller salary expense of 100 makes branch net profit 40 without changing stakeholder profit. Return one unit: stakeholder profit 20 and branch gross profit 70. The salary remains an expense.

### Purchase cost and old records

Purchase valuation uses a moving weighted average for each product/location, **not FIFO**. Example: 5 units at 80 plus 5 at 90 yields a purchase cost of 85. This does not change the existing rule that new/manual baseline prices replace current baseline prices. A sale freezes the purchase average alongside its baseline and actual selling price. Transfers freeze purchase cost when shipped; receipts and physical transfer returns carry that original cost. Accepted sales returns restore inventory at the original sale cost. Cost calculations keep six decimal places; quantity uses three and selling/baseline amounts use four. Use consistent display rounding without altering stored values.

Existing stock/sales have unknown purchase costs after migration; the patch does not guess them. Stakeholder uses `GET /api/stock-costs` and `POST /api/stock-costs/initialize` to record a verified weighted average for an unknown stock balance, with its expected quantity and a reason. Online sales from an unknown-cost balance return 409 until initialized. Known costs cannot be overwritten through initialization.

Existing sales with unknown cost make stakeholder `grossProfit` and `netPurchaseCost` null, with `missingCostEntries > 0`. Branch profit is still available because baseline and selling snapshots already exist. Stakeholder inspects `GET /api/branches/{branchId}/sales/{saleId}/accounting` and may fill a missing original cost once via `PUT /api/branches/{branchId}/sales/{saleId}/lines/{lineId}/purchase-cost`, with verified historical cost and reason. This records who/when/why, never changes existing baseline/selling prices, and cannot overwrite an already captured purchase cost. Exact retries succeed. Filling historical cost does not revalue current stock or transfers; verify their opening costs independently if unknown. Missing-cost entries count sale/return contributions, not unique products.

An offline sale uses cost history effective at its reported sale time. If cost history changed afterwards, normal sync returns 409 for stakeholder reconciliation. If no historical cost can be established, an accepted historical sale remains cost-incomplete until verified; today's cost is never substituted. Sync pending sales before receiving/adjusting stock where possible. Reconciliation adjusts remaining inventory value using the original cost; impossible negative value returns 409 for review.

Historical non-restocked returns, if any were already recorded before this update, are retained. Replaying an identical old request has no new effects. Their baseline/purchase cost is not reversed because no stock returned. No new damaged returns are accepted.

### Branch expenses and salaries

Assigned managers and the stakeholder use `POST /api/branches/{branchId}/expenses` with `clientExpenseId`, category `Salary` or `Other`, positive `amount`, UTC `occurredAtUtc`, and `description`. Optional `employeeUserId` must identify an active seller assigned to that branch. For a historical/grouped salary, omit the employee ID and describe the employee/period. Salaries are recorded manually; the API does not run payroll or pay employees automatically.

`GET .../expenses` lists expenses including voided records. Identical create retries return the same entry; reused identifiers with different data return 409. To correct a mistaken entry, `POST .../expenses/{expenseId}/void` with a reason, then create its replacement with a new identifier. Voiding preserves the record, actor, timestamp, and reason; it removes the expense from its original reporting period. This corrects historical reports rather than creating a current-period refund. Sellers cannot create, view, or void expenses.

### Standalone stakeholder expenses

Stakeholder-paid rent and other spending are recorded in the separate `StakeholderExpenses` table through `POST /api/stakeholder-expenses`. Only stakeholders can create/read/void these entries. Category is `Rent` or `Other`; record amount, UTC expense date and description, with optional branch/payee/reference. The branch reference is descriptive and does not charge its manager.

**This register has no effect on either profit report.** Seller salaries remain in branch expenses and keep their existing branch-net-profit deduction. New Rent entries in branch expenses are rejected even if you previously added that category locally. Existing records are preserved; [the update guide](../stakeholder-expenses-update.md) explains how to correct rent already entered as a branch expense.

`GET /api/stakeholder-expenses` provides pagination and optional branch/category/date/search filters. `activeAmount` totals matching non-voided expenses across all pages, independently of profit. Voids are hidden unless `includeVoided=true`; their amounts never contribute. `GET /api/stakeholder-expenses/{expenseId}` reads one entry. `POST /api/stakeholder-expenses/{expenseId}/void` requires a reason and preserves audit history. Exact retries are idempotent. To correct a record, void and create a replacement using a new client identifier. No payment is executed by recording an expense.

## 9. Product photos

Stakeholder uploads multipart form data to `POST /api/products/{productId}/photos`; field `file` contains the image and `sortOrder` controls ordering. Upload limit is 5 MiB (request limit 6 MiB), decoded pixel limit 20 million, normalized display longest dimension 1600 pixels. New photos are WebP, stripped of source metadata, and compressed to at most 150 KiB; dimensions/quality can decrease to fit. Existing photos are preserved until replaced.

Use `GET .../photos` for metadata and the returned content route for bytes. All three roles can read shared product photos; stakeholder alone uploads/reorders/deletes. Files live in private local storage or S3, with metadata in PostgreSQL. Content uses ETag/Last-Modified for conditional requests. A seller app can download/cache the same photo once for offline display; it does not need a separately uploaded offline picture. Cache refresh and deletion handling are client responsibilities, especially after long offline periods.

## 10. Errors, paging, and support

| Status | Client action |
|---|---|
| 200/201/204 | Successful read/write; persist returned IDs/acknowledgments. |
| 400 | Review validation/problem details; do not silently alter completed offline transactions. |
| 401 | Log in again or use the valid current refresh token where appropriate. |
| 403 | Role/branch permission denied. |
| 404 | Resource not found or not visible to this user. |
| 409 | State, revision, stock, uniqueness, or concurrency conflict. Review detail; retry identical idempotent request only when appropriate. |
| 413 | Photo request exceeds server upload limit. |
| 429 | Wait before trying again; do not spin retry loops. |
| 500/503 | Preserve uncertain write payload/ID; capture traceId and investigate server/database availability. |

Problem responses may include `status`, `title`, `detail`, and `traceId`. Do not show raw technical logs to sellers. List endpoints use `pageNumber`, `pageSize`, `items`, `totalCount`, and page metadata; limits/defaults and filters vary by endpoint, so consult the reference. Return/adjustment/expense/cost/user/purchase/transfer lists are paged but do not implement the generic `search` field.

See `reference.md` and `openapi.json` for routes and schemas. Retrieve live `/openapi/v1.json` using a stakeholder token after future updates. `Batch3.http` is the final manual acceptance file; deployment/backup details are in `../deployment/production.md`.
