# MarketApp Batch 1 sales guide

Batch 1 adds online sales to the API you finished through product photos. The API creates immutable sales invoices, records the prices used at sale time, deducts available stock, and exposes branch-scoped invoice history. Sellers can sell at the manager's minimum price or higher. They cannot read baseline prices through the new catalog or sales responses.

This delivery is based on GitHub commit `3e74404`, named `product photo`. It contains only Batch 1. Returns, offline queues and reconciliation, inventory adjustments, profit reports, and manager profit shares will arrive in the next two batches.

## Install this batch

1. Back up your current project and database. Copy `marketapp-batch1-sales.patch` and `Apply-Batch1.bat` into your EXISTING solution folder beside `MarketApp.sln`. Close the running API, run the BAT file, and reopen your existing solution. The script needs Git for Windows. It checks the complete patch before applying it and stops on conflicts. Use a copy of your current database for our first tests.
2. Keep your existing database connection and JWT signing key. The API's user-secrets identifier is unchanged, so secrets configured on the same Windows account are still available. The patch leaves your appsettings files and existing HTTP examples unchanged. Replace the placeholders only in the new Sales.http file. Keep your existing product photo directory or S3 configuration.
3. Open **Terminal / PowerShell** in the folder containing `MarketApp.sln`. Paths below include `src`; do not run them from inside the API project folder.

```powershell
dotnet restore MarketApp.sln
dotnet build MarketApp.sln
dotnet tool restore
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet ef database update --project src/MarketApp.Infrastructure --startup-project src/MarketApp.Api
```

The included migration is `20261008053608_AddSalesInvoices`. **Do not create another migration.** It follows `20261004094238_CompleteProductPhotoStorage`. EF applies any unapplied earlier migrations before this one. If your database or project has a different migration history, resolve that difference before applying this package; do not delete migration history rows.

In Visual Studio's **Package Manager Console**, the alternative to the database-update command is:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
Update-Database -Project MarketApp.Infrastructure -StartupProject MarketApp.Api
```

Use one method. The complete source projects must already be loaded in the solution. The SQL in `docs/batch-1-upgrade.sql` is for review or a controlled upgrade from the completed photo migration; it is not an additional step after Update-Database.

Start the API from Terminal:

```powershell
dotnet run --project src/MarketApp.Api --launch-profile https
```

The existing HTTPS launch profile uses `https://localhost:7126`. If your developer certificate needs trust, run `dotnet dev-certs https --trust`. Do not rerun identity initialization or replace your JWT signing key on an existing working installation.

## What changed

- `SalesInvoice` is the aggregate root; `SalesInvoiceLine` belongs to its invoice. `IUnitOfWork.SalesInvoices` follows the same pattern as purchase invoices. A separate line repository is unnecessary for creating sales.
- `ISalesService` and `ISalesQueries` are in Application/Interfaces/Services. `SalesService` is in Application/Services; the EF query implementation is in Infrastructure/Services. The existing BranchProductPriceService implementation was moved from Interfaces to Services without changing its behavior.
- `SalesController` adds the six endpoints listed below. The existing branch-access service checks all of them, with the existing JWT/session pipeline.
- `SalesInvoices` and `SalesInvoiceLines` are new database tables. Stock movements gain `SalesInvoiceLineId` and type `Sale` (5). Database constraints associate every sale movement with one invoice line and a negative quantity.
- The Unit of Work now supports a serializable transaction. Invoice creation, stock reduction, and stock movement creation commit together. Conflicting concurrent writes return HTTP 409 and can be retried with the same request identifier.
- Baseline, minimum selling price, actual selling price, price revision, product name, and SKU are stored as historical snapshots. Later pricing or product edits do not rewrite old invoices.
- The solution includes API integration tests and an HTTP request file. Existing pricing and photo implementations are retained.

Invoices cannot be edited or deleted through this batch. A refund or stock restoration will use the return workflow in Batch 2. Do not change posted invoice rows manually to correct a sale.

## Rules for a sale

A sale contains 1–100 different products from one active stock location in the selected branch. The branch, location, and products must be active. Both baseline and minimum selling price must be configured. Quantity must be greater than zero, with up to three decimal places and at most 1,000,000 per line. Selling unit price accepts up to four decimal places and at most 1,000,000,000.

The seller submits the current `priceRevision` received from the catalog. A stale revision returns 409 before stock is deducted. Refresh the catalog and review the selling price before sending a new sale. Charging below the configured minimum returns 409; charging at or above it succeeds when stock is available.

Stock can become exactly zero. A sale exceeding available stock fails as a whole. With several products, one invalid line prevents the entire invoice from being posted.

This batch records online sales using server UTC time. The client does not choose the seller or sale timestamp. The authenticated user is recorded as the seller. Historical offline submission and reconciliation are reserved for Batch 2.

Every new sale needs a new `clientSaleId` generated by the client. If a response is lost, resend the **same identifier and unchanged body using the same user**. A successful retry returns the original invoice with HTTP 200. It does not deduct stock again. Reusing the identifier with different details or a different user returns 409. First-time success returns 201 and a Location header. Concurrent attempts can return 409; retry the unchanged request to discover whether it already succeeded.

Totals use decimal quantity multiplied by decimal unit price. This batch does not add tax, discounts, currency conversion, or a currency rounding policy.

## Roles and visibility

| Action | Seller | Branch manager | Stakeholder |
|---|---|---|---|
| Read catalog, active locations, and stock | Assigned active branches | Assigned active branches | Existing branches |
| Post a sale | Assigned active branches | Assigned active branches | Active branches |
| Read invoice list and details | Own invoices in assigned branch | All invoices in assigned branch | All invoices in selected branch |
| Read baseline in sales responses | No | No | No |
| Change baseline | No | No | Existing management endpoint |
| Change minimum selling price | No | Existing assigned-branch endpoint | Existing management endpoint |

Managers and stakeholders retain the existing management pricing endpoints. The new invoice DTO intentionally uses the same seller-safe format for every role. Detailed profit reporting comes in Batch 2.

Missing or invalid authentication returns 401. A user without branch access receives 403. A seller requesting another seller's invoice receives 404. These restrictions apply to both lists and individual invoice requests.

## Endpoint reference

All six routes start with `/api/branches/{branchId}` and require `Authorization: Bearer YOUR_ACCESS_TOKEN`.

| Method | Suffix | Result |
|---|---|---|
| GET | `/catalog` | Product ID, name, SKU, minimum price, revision, ready photo metadata |
| GET | `/catalog/locations` | Active location IDs, names, and codes in this branch |
| GET | `/catalog/stock` | Product and location IDs with available quantities |
| POST | `/sales` | Create sale, or return original invoice for an identical retry |
| GET | `/sales` | Invoice history, restricted by role |
| GET | `/sales/{saleId}` | One invoice with its original product and selling-price snapshots |

List endpoints accept `pageNumber` (default 1, max 1,000,000) and `pageSize` (default 25, max 100). The catalog also accepts `search` for an exact SKU or product-name substring. Stock accepts `locationId` to filter the branch's balances. An out-of-branch location filter returns an empty list and never exposes that location's stock. Products without a configured minimum can appear in the catalog; they cannot be sold until pricing is configured. Name substring matching follows PostgreSQL's case-sensitive comparison.

List responses contain `items`, `totalCount`, `pageNumber`, `pageSize`, `totalPages`, `hasPreviousPage`, and `hasNextPage`. Invoice history sorts newest first, with ID as a tie breaker.

Example sale body, replacing the GUIDs and revision with real values:

```json
{
  "clientSaleId": "11111111-1111-4111-8111-111111111111",
  "inventoryLocationId": "22222222-2222-4222-8222-222222222222",
  "notes": "Test sale",
  "lines": [
    {
      "productId": "33333333-3333-4333-8333-333333333333",
      "quantity": 2,
      "sellingUnitPrice": 170,
      "priceRevision": 1
    }
  ]
}
```

The example assumes the current minimum is at most 170 and at least two units are available. Generate a real new identifier with `[guid]::NewGuid()` in PowerShell. Keep that value unchanged for the retry test.

The response contains invoice ID and number, clientSaleId, branch and location IDs, authenticated seller ID, UTC timestamps, notes, total, and lines. Each line contains ID, product ID, original name and SKU, quantity, actual selling price, and total. Baseline and minimum snapshots are stored in the database but excluded from this response.

The principal status codes are 201 (new sale), 200 (read or replay), 400 (malformed input), 401 (authentication), 403 (branch or role access), 404 (invoice unavailable to this user), and 409 (stock, pricing, or concurrent-data conflict). A 500 response means an unexpected error; check the API log before deciding what to retry.

## Test together in this order

Use a test database and existing users for Stakeholder, BranchManager, and Seller. The seller and manager must be assigned to the branch. Prepare one active product with ten available units, baseline 100, and minimum selling price 150. You can instead use existing test values and calculate the expected totals accordingly.

Open `src/MarketApp.Api/ControllersTester/Sales.http`. Set the URL, access tokens, branch ID, product ID, stock location ID, and latest price revision. The file includes a username-login request. Copy the returned token; do not leave the example placeholders unchanged.

1. **Discover data.** As seller, GET catalog, locations, and stock. Confirm the expected product, branch location, price revision, and quantity. No baseline should appear.
2. **Post a sale.** Sell two units at 170. Expect 201, total 340, and stock 8. Save the returned invoice ID and line ID.
3. **Repeat the same request.** Keep clientSaleId and the body unchanged. Expect 200 with the same invoice ID; stock stays 8.
4. **Misuse an identifier.** Change the quantity while keeping that successful clientSaleId. Expect 409; stock stays 8.
5. **Reject invalid prices and quantities.** With a new clientSaleId, try price 149, quantity 9, and quantity 0 separately. Expect 409, 409, and 400. None should change stock.
6. **Check atomic failure.** Send two product lines where the second product is missing or has insufficient stock. Expect the entire sale to fail, with no deduction for the first product.
7. **Check price updates.** Retain an old revision, update the minimum through the manager endpoint, and send a new sale with the old revision. Expect 409. Reload the catalog before trying the new price and revision. The first invoice still totals 340.
8. **Check access.** Another seller in the branch should get 404 for the first seller's invoice and should not see it in their list. The branch manager should see it. A seller assigned to a different branch should get 403 for this branch's routes. Management pricing routes should reject sellers.
9. **Sell the remaining quantity.** Reload stock and prices, generate a new clientSaleId, and sell exactly what remains at a permitted price. Expect stock 0. A further sale should return 409.
10. **Check the audit trail.** As stakeholder, use the existing stock movement endpoint. The new negative movement has type `Sale` and its `salesInvoiceLineId` points to the posted line.

Stop here and review our results before Batch 2. Preserve the database and source backup until the tests pass. Migration rollback after sales have been posted is not a stock correction mechanism; restore a consistent backup if a full rollback is required.

## Implementation files to review

- Domain: `Entity/Sales/SalesInvoice.cs`, `SalesInvoiceLine.cs`, `Entity/Inventory/StockMovement.cs`, and `Enums/StockMovementType.cs`.
- Application: `DTOs/Sales/SalesDtos.cs`, `Interfaces/Services/ISalesService.cs`, `ISalesQueries.cs`, `Services/SalesService.cs`, and Unit of Work contracts.
- Infrastructure: `Services/SalesQueries.cs`, `Persistence/UnitOfWork.cs`, `Configurations/SalesConfigurations.cs`, `StockMovementConfiguration.cs`, `DatabaseConflictHandler.cs`, and the AddSalesInvoices migration and snapshot.
- API: `Controllers/SalesController.cs`, exception registration, and `ControllersTester/Sales.http`.
- Tests: `tests/MarketApp.Api.Tests/SalesApiTests.cs`.

Technical references: Microsoft EF Core documentation on [transactions](https://learn.microsoft.com/en-us/ef/core/saving/transactions) and [managing migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/managing). The verification report distinguishes generated migration checks from execution against PostgreSQL.
