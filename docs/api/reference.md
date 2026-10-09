# API reference

Generated from endpoint metadata after the profit-accounting update. `openapi.json` contains the full request/response schemas and can be imported into an API client. Set your HTTPS base URL; the exported server URL is relative (`/`). Live specification: `/openapi/v1.json`, stakeholder token required.

Read [usage-guide.md](usage-guide.md) for business rules and [the update guide](../profit-accounting-update.md) for installation and opening costs. Role checks also enforce branch assignments and active sessions. Common errors: 400 validation, 401 login required, 403 permission denied, 404 not found, 409 conflict, 429 rate limit, 500/503 server/dependency failure.

## POST /api/auth/confirm-email

**Access:** Anonymous.

**Body:** `application/json` → `ConfirmEmailRequestDto`; `text/json` → `ConfirmEmailRequestDto`; `application/*+json` → `ConfirmEmailRequestDto`.

**Responses:** 204 (no body).

## POST /api/auth/forgot-password

**Access:** Anonymous.

**Body:** `application/json` → `ForgotPasswordRequestDto`; `text/json` → `ForgotPasswordRequestDto`; `application/*+json` → `ForgotPasswordRequestDto`.

**Responses:** 200 → MessageResponse.

## POST /api/auth/login

**Access:** Anonymous.

**Body:** `application/json` → `LoginRequestDto`; `text/json` → `LoginRequestDto`; `application/*+json` → `LoginRequestDto`.

**Responses:** 200 → AuthResponseDto.

## POST /api/auth/logout

**Access:** Any authenticated role.

**Responses:** 204 (no body).

## GET /api/auth/me

**Access:** Any authenticated role.

**Responses:** 200 → CurrentUserResponse.

## POST /api/auth/refresh

**Access:** Anonymous.

**Body:** `application/json` → `RefreshRequestDto`; `text/json` → `RefreshRequestDto`; `application/*+json` → `RefreshRequestDto`.

**Responses:** 200 → AuthResponseDto.

## POST /api/auth/request-email-verification

**Access:** Any authenticated role.

**Responses:** 200 → MessageResponse.

## POST /api/auth/reset-password

**Access:** Anonymous.

**Body:** `application/json` → `ResetPasswordRequestDto`; `text/json` → `ResetPasswordRequestDto`; `application/*+json` → `ResetPasswordRequestDto`.

**Responses:** 204 (no body).

## GET /api/branches

**Access:** Stakeholder only.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `PageNumber` | query | integer or string (int32) | No |
| `PageSize` | query | integer or string (int32) | No |
| `IsActive` | query | boolean | No |

**Responses:** 200 → PagedResultOfBranchDto.

## POST /api/branches

**Access:** Stakeholder only.

**Body:** `application/json` → `SaveBranchDto`; `text/json` → `SaveBranchDto`; `application/*+json` → `SaveBranchDto`.

**Responses:** 200 → BranchDto.

## GET /api/branches/{branchId}/catalog

**Access:** Stakeholder, assigned BranchManager or Seller; sellers read only their own invoices.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `branchId` | path | string (uuid) | Yes |
| `PageNumber` | query | integer or string (int32) | No |
| `PageSize` | query | integer or string (int32) | No |
| `Search` | query | string | No |

**Responses:** 200 → PagedResultOfCatalogProductDto.

## GET /api/branches/{branchId}/catalog/locations

**Access:** Stakeholder, assigned BranchManager or Seller; sellers read only their own invoices.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `branchId` | path | string (uuid) | Yes |
| `PageNumber` | query | integer or string (int32) | No |
| `PageSize` | query | integer or string (int32) | No |
| `Search` | query | string | No |

**Responses:** 200 → PagedResultOfCatalogLocationDto.

## GET /api/branches/{branchId}/catalog/stock

**Access:** Stakeholder, assigned BranchManager or Seller; sellers read only their own invoices.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `branchId` | path | string (uuid) | Yes |
| `locationId` | query | string (uuid) | No |
| `PageNumber` | query | integer or string (int32) | No |
| `PageSize` | query | integer or string (int32) | No |
| `Search` | query | string | No |

**Responses:** 200 → PagedResultOfCatalogStockDto.

## GET /api/branches/{branchId}/expenses

**Access:** Stakeholder or assigned BranchManager.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `branchId` | path | string (uuid) | Yes |
| `PageNumber` | query | integer or string (int32) | No |
| `PageSize` | query | integer or string (int32) | No |
| `Search` | query | string | No |

**Responses:** 200 → PagedResultOfExpenseDto.

## POST /api/branches/{branchId}/expenses

**Access:** Stakeholder or assigned BranchManager.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `branchId` | path | string (uuid) | Yes |

**Body:** `application/json` → `CreateExpenseDto`; `text/json` → `CreateExpenseDto`; `application/*+json` → `CreateExpenseDto`.

**Responses:** 200 → ExpenseDto.

## POST /api/branches/{branchId}/expenses/{expenseId}/void

**Access:** Stakeholder or assigned BranchManager.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `branchId` | path | string (uuid) | Yes |
| `expenseId` | path | string (uuid) | Yes |

**Body:** `application/json` → `VoidExpenseDto`; `text/json` → `VoidExpenseDto`; `application/*+json` → `VoidExpenseDto`.

**Responses:** 200 → ExpenseDto.

## GET /api/branches/{branchId}/manager-profit-shares

**Access:** Stakeholder only; retired (410).

| Parameter | Location | Type | Required |
|---|---|---|---|
| `branchId` | path | string (uuid) | Yes |

**Responses:** 410 → ProblemDetails.

## POST /api/branches/{branchId}/manager-profit-shares

**Access:** Stakeholder only; retired (410).

| Parameter | Location | Type | Required |
|---|---|---|---|
| `branchId` | path | string (uuid) | Yes |

**Responses:** 410 → ProblemDetails.

## GET /api/branches/{branchId}/product-prices/{productId}

**Access:** Stakeholder or assigned BranchManager.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `branchId` | path | string (uuid) | Yes |
| `productId` | path | string (uuid) | Yes |

**Responses:** 200 → BranchProductPriceDto.

## PUT /api/branches/{branchId}/product-prices/{productId}/baseline

**Access:** Stakeholder only.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `branchId` | path | string (uuid) | Yes |
| `productId` | path | string (uuid) | Yes |

**Body:** `application/json` → `UpdateBranchPriceDto`; `text/json` → `UpdateBranchPriceDto`; `application/*+json` → `UpdateBranchPriceDto`.

**Responses:** 200 → BranchProductPriceDto.

## GET /api/branches/{branchId}/product-prices/{productId}/history

**Access:** Stakeholder or assigned BranchManager.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `branchId` | path | string (uuid) | Yes |
| `productId` | path | string (uuid) | Yes |
| `PageNumber` | query | integer or string (int32) | No |
| `PageSize` | query | integer or string (int32) | No |

**Responses:** 200 → PagedResultOfBranchProductPriceHistoryDto.

## PUT /api/branches/{branchId}/product-prices/{productId}/minimum-selling-price

**Access:** Stakeholder or assigned BranchManager.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `branchId` | path | string (uuid) | Yes |
| `productId` | path | string (uuid) | Yes |

**Body:** `application/json` → `UpdateBranchPriceDto`; `text/json` → `UpdateBranchPriceDto`; `application/*+json` → `UpdateBranchPriceDto`.

**Responses:** 200 → BranchProductPriceDto.

## GET /api/branches/{branchId}/reports/profit

**Access:** Stakeholder or assigned BranchManager.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `branchId` | path | string (uuid) | Yes |
| `fromUtc` | query | string (date-time) | No |
| `toUtc` | query | string (date-time) | No |

**Responses:** 200 → BranchReportDto.

## POST /api/branches/{branchId}/sales

**Access:** Stakeholder, assigned BranchManager or Seller; sellers read only their own invoices.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `branchId` | path | string (uuid) | Yes |

**Body:** `application/json` → `CreateSaleDto`; `text/json` → `CreateSaleDto`; `application/*+json` → `CreateSaleDto`.

**Responses:** 201 → SaleDto; 200 → SaleDto.

## GET /api/branches/{branchId}/sales

**Access:** Stakeholder, assigned BranchManager or Seller; sellers read only their own invoices.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `branchId` | path | string (uuid) | Yes |
| `PageNumber` | query | integer or string (int32) | No |
| `PageSize` | query | integer or string (int32) | No |
| `Search` | query | string | No |

**Responses:** 200 → PagedResultOfSaleDto.

## GET /api/branches/{branchId}/sales-returns

**Access:** Stakeholder or assigned BranchManager.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `branchId` | path | string (uuid) | Yes |
| `PageNumber` | query | integer or string (int32) | No |
| `PageSize` | query | integer or string (int32) | No |
| `Search` | query | string | No |

**Responses:** 200 → PagedResultOfReturnDto.

## POST /api/branches/{branchId}/sales/reconcile

**Access:** Stakeholder only.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `branchId` | path | string (uuid) | Yes |

**Body:** `application/json` → `ReconcileSaleDto`; `text/json` → `ReconcileSaleDto`; `application/*+json` → `ReconcileSaleDto`.

**Responses:** 201 → SaleDto; 200 → SaleDto.

## POST /api/branches/{branchId}/sales/sync

**Access:** Stakeholder, assigned BranchManager or Seller; sellers read only their own invoices.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `branchId` | path | string (uuid) | Yes |

**Body:** `application/json` → `SyncSaleDto`; `text/json` → `SyncSaleDto`; `application/*+json` → `SyncSaleDto`.

**Responses:** 201 → SaleDto; 200 → SaleDto.

## GET /api/branches/{branchId}/sales/{saleId}

**Access:** Stakeholder, assigned BranchManager or Seller; sellers read only their own invoices.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `branchId` | path | string (uuid) | Yes |
| `saleId` | path | string (uuid) | Yes |

**Responses:** 200 → SaleDto.

## GET /api/branches/{branchId}/sales/{saleId}/accounting

**Access:** Stakeholder only.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `branchId` | path | string (uuid) | Yes |
| `saleId` | path | string (uuid) | Yes |

**Responses:** 200 → array of SaleAccountingLineDto.

## PUT /api/branches/{branchId}/sales/{saleId}/lines/{lineId}/purchase-cost

**Access:** Stakeholder only.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `branchId` | path | string (uuid) | Yes |
| `saleId` | path | string (uuid) | Yes |
| `lineId` | path | string (uuid) | Yes |

**Body:** `application/json` → `RecordHistoricalCostDto`; `text/json` → `RecordHistoricalCostDto`; `application/*+json` → `RecordHistoricalCostDto`.

**Responses:** 204 (no body).

## POST /api/branches/{branchId}/sales/{saleId}/returns

**Access:** Stakeholder or assigned BranchManager.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `branchId` | path | string (uuid) | Yes |
| `saleId` | path | string (uuid) | Yes |

**Body:** `application/json` → `CreateReturnDto`; `text/json` → `CreateReturnDto`; `application/*+json` → `CreateReturnDto`.

**Responses:** 200 → ReturnDto.

## GET /api/branches/{branchId}/selling-prices/{productId}

**Access:** Stakeholder only.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `branchId` | path | string (uuid) | Yes |
| `productId` | path | string (uuid) | Yes |

**Responses:** 200 → SellerProductPriceDto.

## GET /api/branches/{id}

**Access:** Stakeholder only.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `id` | path | string (uuid) | Yes |

**Responses:** 200 → BranchDto.

## PUT /api/branches/{id}

**Access:** Stakeholder only.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `id` | path | string (uuid) | Yes |

**Body:** `application/json` → `SaveBranchDto`; `text/json` → `SaveBranchDto`; `application/*+json` → `SaveBranchDto`.

**Responses:** 200 → BranchDto.

## GET /api/categories

**Access:** Stakeholder only.

**Responses:** 200 → array of CategoryDto.

## POST /api/categories

**Access:** Stakeholder only.

**Body:** `application/json` → `SaveCategoryDto`; `text/json` → `SaveCategoryDto`; `application/*+json` → `SaveCategoryDto`.

**Responses:** 200 → CategoryDto.

## GET /api/categories/{id}

**Access:** Stakeholder only.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `id` | path | string (uuid) | Yes |

**Responses:** 200 → CategoryDto.

## PUT /api/categories/{id}

**Access:** Stakeholder only.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `id` | path | string (uuid) | Yes |

**Body:** `application/json` → `SaveCategoryDto`; `text/json` → `SaveCategoryDto`; `application/*+json` → `SaveCategoryDto`.

**Responses:** 200 → CategoryDto.

## DELETE /api/categories/{id}

**Access:** Stakeholder only.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `id` | path | string (uuid) | Yes |

**Responses:** 204 (no body).

## GET /api/inventory-locations

**Access:** Stakeholder only.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `PageNumber` | query | integer or string (int32) | No |
| `PageSize` | query | integer or string (int32) | No |
| `BranchId` | query | string (uuid) | No |
| `IsActive` | query | boolean | No |

**Responses:** 200 → PagedResultOfInventoryLocationDto.

## POST /api/inventory-locations

**Access:** Stakeholder only.

**Body:** `application/json` → `CreateInventoryLocationDto`; `text/json` → `CreateInventoryLocationDto`; `application/*+json` → `CreateInventoryLocationDto`.

**Responses:** 200 → InventoryLocationDto.

## GET /api/inventory-locations/{id}

**Access:** Stakeholder only.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `id` | path | string (uuid) | Yes |

**Responses:** 200 → InventoryLocationDto.

## PUT /api/inventory-locations/{id}

**Access:** Stakeholder only.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `id` | path | string (uuid) | Yes |

**Body:** `application/json` → `SaveInventoryLocationDto`; `text/json` → `SaveInventoryLocationDto`; `application/*+json` → `SaveInventoryLocationDto`.

**Responses:** 200 → InventoryLocationDto.

## GET /api/products

**Access:** Stakeholder only.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `PageNumber` | query | integer or string (int32) | No |
| `PageSize` | query | integer or string (int32) | No |
| `CategoryId` | query | string (uuid) | No |
| `IsActive` | query | boolean | No |

**Responses:** 200 → PagedResultOfProductDto.

## POST /api/products

**Access:** Stakeholder only.

**Body:** `application/json` → `SaveProductDto`; `text/json` → `SaveProductDto`; `application/*+json` → `SaveProductDto`.

**Responses:** 200 → ProductDto.

## GET /api/products/{id}

**Access:** Stakeholder only.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `id` | path | string (uuid) | Yes |

**Responses:** 200 → ProductDto.

## PUT /api/products/{id}

**Access:** Stakeholder only.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `id` | path | string (uuid) | Yes |

**Body:** `application/json` → `SaveProductDto`; `text/json` → `SaveProductDto`; `application/*+json` → `SaveProductDto`.

**Responses:** 200 → ProductDto.

## GET /api/products/{productId}/photos

**Access:** Any authenticated role.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `productId` | path | string (uuid) | Yes |

**Responses:** 200 → array of ProductPhotoDto.

## POST /api/products/{productId}/photos

**Access:** Stakeholder only.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `productId` | path | string (uuid) | Yes |

**Body:** `multipart/form-data` → `object`.

**Responses:** 201 → ProductPhotoDto.

## DELETE /api/products/{productId}/photos/{photoId}

**Access:** Stakeholder only.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `productId` | path | string (uuid) | Yes |
| `photoId` | path | string (uuid) | Yes |

**Responses:** 204 (no body).

## GET /api/products/{productId}/photos/{photoId}/content

**Access:** Any authenticated role.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `productId` | path | string (uuid) | Yes |
| `photoId` | path | string (uuid) | Yes |

**Responses:** 200 (no body).

## PUT /api/products/{productId}/photos/{photoId}/order

**Access:** Stakeholder only.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `productId` | path | string (uuid) | Yes |
| `photoId` | path | string (uuid) | Yes |

**Body:** `application/json` → `UpdateProductPhotoOrderRequest`; `text/json` → `UpdateProductPhotoOrderRequest`; `application/*+json` → `UpdateProductPhotoOrderRequest`.

**Responses:** 204 (no body).

## GET /api/purchase-invoices

**Access:** Stakeholder only.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `PageNumber` | query | integer or string (int32) | No |
| `PageSize` | query | integer or string (int32) | No |
| `Search` | query | string | No |

**Responses:** 200 → PagedResultOfPurchaseInvoiceDto.

## POST /api/purchase-invoices

**Access:** Stakeholder only.

**Body:** `application/json` → `CreatePurchaseInvoiceDto`; `text/json` → `CreatePurchaseInvoiceDto`; `application/*+json` → `CreatePurchaseInvoiceDto`.

**Responses:** 200 → PurchaseInvoiceDto.

## GET /api/purchase-invoices/{id}

**Access:** Stakeholder only.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `id` | path | string (uuid) | Yes |

**Responses:** 200 → PurchaseInvoiceDto.

## POST /api/purchase-invoices/{id}/post

**Access:** Stakeholder only.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `id` | path | string (uuid) | Yes |

**Responses:** 200 → PurchaseInvoiceDto.

## GET /api/reports/stakeholder-profit

**Access:** Stakeholder only.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `branchId` | query | string (uuid) | No |
| `fromUtc` | query | string (date-time) | No |
| `toUtc` | query | string (date-time) | No |

**Responses:** 200 → StakeholderProfitDto.

## POST /api/stock-adjustments

**Access:** Stakeholder only.

**Body:** `application/json` → `CreateAdjustmentDto`; `text/json` → `CreateAdjustmentDto`; `application/*+json` → `CreateAdjustmentDto`.

**Responses:** 200 → AdjustmentDto.

## GET /api/stock-adjustments

**Access:** Stakeholder only.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `PageNumber` | query | integer or string (int32) | No |
| `PageSize` | query | integer or string (int32) | No |
| `Search` | query | string | No |

**Responses:** 200 → PagedResultOfAdjustmentDto.

## GET /api/stock-costs

**Access:** Stakeholder only.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `locationId` | query | string (uuid) | No |
| `PageNumber` | query | integer or string (int32) | No |
| `PageSize` | query | integer or string (int32) | No |
| `Search` | query | string | No |

**Responses:** 200 → PagedResultOfStockCostDto.

## POST /api/stock-costs/initialize

**Access:** Stakeholder only.

**Body:** `application/json` → `InitializeStockCostDto`; `text/json` → `InitializeStockCostDto`; `application/*+json` → `InitializeStockCostDto`.

**Responses:** 200 → StockCostDto.

## GET /api/stock-transfers

**Access:** Stakeholder only.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `PageNumber` | query | integer or string (int32) | No |
| `PageSize` | query | integer or string (int32) | No |
| `Search` | query | string | No |

**Responses:** 200 → PagedResultOfStockTransferDto.

## POST /api/stock-transfers

**Access:** Stakeholder only.

**Body:** `application/json` → `CreateStockTransferDto`; `text/json` → `CreateStockTransferDto`; `application/*+json` → `CreateStockTransferDto`.

**Responses:** 200 → StockTransferDto.

## GET /api/stock-transfers/{id}

**Access:** Stakeholder only.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `id` | path | string (uuid) | Yes |

**Responses:** 200 → StockTransferDto.

## POST /api/stock-transfers/{id}/cancel

**Access:** Stakeholder only.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `id` | path | string (uuid) | Yes |

**Responses:** 200 → StockTransferDto.

## POST /api/stock-transfers/{id}/confirm-return

**Access:** Stakeholder only.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `id` | path | string (uuid) | Yes |

**Responses:** 200 → StockTransferDto.

## POST /api/stock-transfers/{id}/receive

**Access:** Stakeholder only.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `id` | path | string (uuid) | Yes |

**Responses:** 200 → StockTransferDto.

## POST /api/stock-transfers/{id}/request-return

**Access:** Stakeholder only.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `id` | path | string (uuid) | Yes |

**Body:** `application/json` → `RequestStockTransferReturnDto`; `text/json` → `RequestStockTransferReturnDto`; `application/*+json` → `RequestStockTransferReturnDto`.

**Responses:** 200 → StockTransferDto.

## POST /api/stock-transfers/{id}/ship

**Access:** Stakeholder only.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `id` | path | string (uuid) | Yes |

**Responses:** 200 → StockTransferDto.

## GET /api/stock/balances

**Access:** Stakeholder only.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `PageNumber` | query | integer or string (int32) | No |
| `PageSize` | query | integer or string (int32) | No |
| `ProductId` | query | string (uuid) | No |
| `InventoryLocationId` | query | string (uuid) | No |

**Responses:** 200 → PagedResultOfStockBalanceDto.

## GET /api/stock/movements

**Access:** Stakeholder only.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `PageNumber` | query | integer or string (int32) | No |
| `PageSize` | query | integer or string (int32) | No |
| `ProductId` | query | string (uuid) | No |
| `InventoryLocationId` | query | string (uuid) | No |

**Responses:** 200 → PagedResultOfStockMovementDto.

## GET /api/suppliers

**Access:** Stakeholder only.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `PageNumber` | query | integer or string (int32) | No |
| `PageSize` | query | integer or string (int32) | No |
| `IsActive` | query | boolean | No |

**Responses:** 200 → PagedResultOfSupplierDto.

## POST /api/suppliers

**Access:** Stakeholder only.

**Body:** `application/json` → `SaveSupplierDto`; `text/json` → `SaveSupplierDto`; `application/*+json` → `SaveSupplierDto`.

**Responses:** 200 → SupplierDto.

## GET /api/suppliers/{id}

**Access:** Stakeholder only.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `id` | path | string (uuid) | Yes |

**Responses:** 200 → SupplierDto.

## PUT /api/suppliers/{id}

**Access:** Stakeholder only.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `id` | path | string (uuid) | Yes |

**Body:** `application/json` → `SaveSupplierDto`; `text/json` → `SaveSupplierDto`; `application/*+json` → `SaveSupplierDto`.

**Responses:** 200 → SupplierDto.

## GET /api/users

**Access:** Stakeholder only.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `PageNumber` | query | integer or string (int32) | No |
| `PageSize` | query | integer or string (int32) | No |
| `Search` | query | string | No |

**Responses:** 200 → PagedResultOfUserDetailsDto.

## POST /api/users

**Access:** Stakeholder only.

**Body:** `application/json` → `CreateUserRequestDto`; `text/json` → `CreateUserRequestDto`; `application/*+json` → `CreateUserRequestDto`.

**Responses:** 201 → UserDetailsDto.

## GET /api/users/{id}

**Access:** Stakeholder only.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `id` | path | string (uuid) | Yes |

**Responses:** 200 → UserDetailsDto.

## PUT /api/users/{id}/access

**Access:** Stakeholder only.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `id` | path | string (uuid) | Yes |

**Body:** `application/json` → `UpdateUserAccessDto`; `text/json` → `UpdateUserAccessDto`; `application/*+json` → `UpdateUserAccessDto`.

**Responses:** 200 → UserDetailsDto.

## GET /health/live

**Access:** Anonymous.

**Responses:** 200 → HealthResponse.

## GET /health/ready

**Access:** Stakeholder only.

**Responses:** 200 → HealthResponse.

The specification covers 82 operations across 63 paths. Development-only debug routes are omitted from the exported specification.

# Request and response schemas

## AdjustmentDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `id` | string (uuid) | Yes | — |
| `clientAdjustmentId` | string (uuid) | Yes | — |
| `productId` | string (uuid) | Yes | — |
| `inventoryLocationId` | string (uuid) | Yes | — |
| `quantityChange` | number or string (double) | Yes | pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |
| `reason` | string | Yes | — |
| `createdByUserId` | string (uuid) | Yes | — |
| `createdAtUtc` | string (date-time) | Yes | — |

## AuthResponseDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `tokenType` | string | Yes | — |
| `accessToken` | string | Yes | — |
| `expiresIn` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `refreshToken` | string | Yes | — |
| `sessionExpiresAtUtc` | string (date-time) | Yes | — |

## BranchDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `id` | string (uuid) | Yes | — |
| `name` | string | Yes | — |
| `code` | string | Yes | — |
| `address` | null or string | Yes | — |
| `phone` | null or string | Yes | — |
| `isActive` | boolean | Yes | — |

## BranchProductPriceDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `id` | string (uuid) | Yes | — |
| `branchId` | string (uuid) | Yes | — |
| `productId` | string (uuid) | Yes | — |
| `baselineUnitPrice` | null or number or string (double) | Yes | pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |
| `minimumSellingPrice` | null or number or string (double) | Yes | pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |
| `revision` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `updatedAtUtc` | string (date-time) | Yes | — |

## BranchProductPriceHistoryDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `id` | string (uuid) | Yes | — |
| `revision` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `oldBaselineUnitPrice` | null or number or string (double) | Yes | pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |
| `newBaselineUnitPrice` | null or number or string (double) | Yes | pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |
| `oldMinimumSellingPrice` | null or number or string (double) | Yes | pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |
| `newMinimumSellingPrice` | null or number or string (double) | Yes | pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |
| `changeType` | string | Yes | — |
| `reason` | string | Yes | — |
| `changedAtUtc` | string (date-time) | Yes | — |
| `changedByUserId` | null or string (uuid) | Yes | — |
| `stockTransferLineId` | null or string (uuid) | Yes | — |

## BranchReportDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `branchId` | string (uuid) | Yes | — |
| `fromUtc` | string (date-time) | Yes | — |
| `toUtc` | string (date-time) | Yes | — |
| `saleCount` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `salesRevenue` | number or string (double) | Yes | pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |
| `refunds` | number or string (double) | Yes | pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |
| `netRevenue` | number or string (double) | Yes | pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |
| `netBaselineValue` | number or string (double) | Yes | pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |
| `grossProfit` | number or string (double) | Yes | pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |
| `expensesTotal` | number or string (double) | No | pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |
| `netProfit` | number or string (double) | No | pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |

## CatalogLocationDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `id` | string (uuid) | Yes | — |
| `name` | string | Yes | — |
| `code` | string | Yes | — |

## CatalogProductDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `id` | string (uuid) | Yes | — |
| `name` | string | Yes | — |
| `sku` | string | Yes | — |
| `minimumSellingPrice` | null or number or string (double) | Yes | pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |
| `priceRevision` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `photos` | array of ProductPhotoDto | Yes | — |

## CatalogStockDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `productId` | string (uuid) | Yes | — |
| `inventoryLocationId` | string (uuid) | Yes | — |
| `quantity` | number or string (double) | Yes | pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |

## CategoryDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `id` | string (uuid) | Yes | — |
| `name` | string | Yes | — |

## ConfirmEmailRequestDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `userId` | string (uuid) | No | — |
| `token` | string | Yes | minLength: 0; maxLength: 4096 |

## CreateAdjustmentDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `clientAdjustmentId` | string (uuid) | No | — |
| `inventoryLocationId` | string (uuid) | No | — |
| `productId` | string (uuid) | No | — |
| `quantityChange` | number or string (double) | No | minimum: -1000000; maximum: 1000000; pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |
| `reason` | string | Yes | minLength: 3; maxLength: 1000 |

## CreateExpenseDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `clientExpenseId` | string (uuid) | No | — |
| `category` | string | Yes | pattern: ^(Salary\|Other)$ |
| `employeeUserId` | null or string (uuid) | No | — |
| `amount` | number or string (double) | No | minimum: 0.0001; maximum: 1000000000000; pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |
| `occurredAtUtc` | string (date-time) | No | — |
| `description` | string | Yes | minLength: 3; maxLength: 1000 |

## CreateInventoryLocationDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `branchId` | null or string (uuid) | No | — |
| `name` | string | Yes | minLength: 0; maxLength: 100 |
| `code` | string | Yes | minLength: 0; maxLength: 32 |
| `isActive` | boolean | No | — |

## CreatePurchaseInvoiceDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `supplierId` | string (uuid) | No | — |
| `inventoryLocationId` | string (uuid) | No | — |
| `invoiceDate` | string (date) | No | — |
| `supplierInvoiceNumber` | null or string | No | minLength: 0; maxLength: 100 |
| `notes` | null or string | No | minLength: 0; maxLength: 1000 |
| `lines` | array of CreatePurchaseInvoiceLineDto | Yes | minItems: 1; maxItems: 200 |

## CreatePurchaseInvoiceLineDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `productId` | string (uuid) | No | — |
| `quantity` | number or string (double) | No | minimum: 0.001; maximum: 1000000000; pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |
| `unitCost` | number or string (double) | No | minimum: 0; maximum: 1000000000; pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |

## CreateReturnDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `clientReturnId` | string (uuid) | No | — |
| `reason` | string | Yes | minLength: 3; maxLength: 1000 |
| `lines` | array of CreateReturnLineDto | Yes | minItems: 1; maxItems: 100 |

## CreateReturnLineDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `salesInvoiceLineId` | string (uuid) | No | — |
| `quantity` | number or string (double) | No | minimum: 0.001; maximum: 1000000; pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |
| `restock` | boolean | No | — |

## CreateSaleDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `clientSaleId` | string (uuid) | No | — |
| `inventoryLocationId` | string (uuid) | No | — |
| `notes` | null or string | No | minLength: 0; maxLength: 1000 |
| `lines` | array of CreateSaleLineDto | Yes | minItems: 1; maxItems: 100 |

## CreateSaleLineDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `productId` | string (uuid) | No | — |
| `quantity` | number or string (double) | No | minimum: 0.001; maximum: 1000000; pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |
| `sellingUnitPrice` | number or string (double) | No | minimum: 0; maximum: 1000000000; pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |
| `priceRevision` | integer or string (int32) | No | minimum: 1; maximum: 2147483647; pattern: ^-?(?:0\|[1-9]\d*)$ |

## CreateStockTransferDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `sourceLocationId` | string (uuid) | No | — |
| `destinationLocationId` | string (uuid) | No | — |
| `notes` | null or string | No | minLength: 0; maxLength: 1000 |
| `lines` | array of CreateStockTransferLineDto | Yes | minItems: 1; maxItems: 200 |

## CreateStockTransferLineDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `productId` | string (uuid) | No | — |
| `quantity` | number or string (double) | No | minimum: 0.001; maximum: 1000000000; pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |
| `baselineUnitPrice` | number or string (double) | No | minimum: 0; maximum: 1000000000; pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |

## CreateUserRequestDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `userName` | string | Yes | minLength: 0; maxLength: 256 |
| `fullName` | string | Yes | minLength: 0; maxLength: 150 |
| `email` | string | Yes | minLength: 0; maxLength: 256 |
| `password` | string | Yes | minLength: 4; maxLength: 1024 |
| `role` | string | Yes | — |
| `branchIds` | array of string (uuid) | Yes | minItems: 1; maxItems: 50 |

## CurrentUserResponse

| Field | Type | Required | Constraints |
|---|---|---|---|
| `userId` | null or string | Yes | — |
| `userName` | null or string | Yes | — |
| `sessionId` | null or string | Yes | — |
| `roles` | array of string | Yes | — |

## ExpenseDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `id` | string (uuid) | Yes | — |
| `branchId` | string (uuid) | Yes | — |
| `clientExpenseId` | string (uuid) | Yes | — |
| `category` | string | Yes | — |
| `employeeUserId` | null or string (uuid) | Yes | — |
| `amount` | number or string (double) | Yes | pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |
| `occurredAtUtc` | string (date-time) | Yes | — |
| `description` | string | Yes | — |
| `createdByUserId` | string (uuid) | Yes | — |
| `createdAtUtc` | string (date-time) | Yes | — |
| `voidedByUserId` | null or string (uuid) | Yes | — |
| `voidedAtUtc` | null or string (date-time) | Yes | — |
| `voidReason` | null or string | Yes | — |

## ForgotPasswordRequestDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `userName` | string | Yes | minLength: 0; maxLength: 256 |

## HealthResponse

| Field | Type | Required | Constraints |
|---|---|---|---|
| `status` | string | Yes | — |

## IFormFile

Type: string (binary).

## InitializeStockCostDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `clientChangeId` | string (uuid) | No | — |
| `productId` | string (uuid) | No | — |
| `inventoryLocationId` | string (uuid) | No | — |
| `expectedQuantity` | number or string (double) | No | minimum: 0; maximum: 1000000000000000.0; pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |
| `purchaseUnitCost` | null or number or string (double) | Yes | minimum: 0; maximum: 1000000000; pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |
| `reason` | string | Yes | minLength: 3; maxLength: 1000 |

## InventoryLocationDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `id` | string (uuid) | Yes | — |
| `name` | string | Yes | — |
| `code` | string | Yes | — |
| `branchId` | null or string (uuid) | Yes | — |
| `branchName` | null or string | Yes | — |
| `isActive` | boolean | Yes | — |

## LoginRequestDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `userName` | string | Yes | minLength: 0; maxLength: 256 |
| `password` | string | Yes | minLength: 0; maxLength: 1024 |

## MessageResponse

| Field | Type | Required | Constraints |
|---|---|---|---|
| `message` | string | Yes | — |

## PagedResultOfAdjustmentDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `items` | array of AdjustmentDto | Yes | — |
| `totalCount` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `pageNumber` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `pageSize` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `totalPages` | integer or string (int32) | No | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `hasPreviousPage` | boolean | No | — |
| `hasNextPage` | boolean | No | — |

## PagedResultOfBranchDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `items` | array of BranchDto | Yes | — |
| `totalCount` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `pageNumber` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `pageSize` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `totalPages` | integer or string (int32) | No | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `hasPreviousPage` | boolean | No | — |
| `hasNextPage` | boolean | No | — |

## PagedResultOfBranchProductPriceHistoryDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `items` | array of BranchProductPriceHistoryDto | Yes | — |
| `totalCount` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `pageNumber` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `pageSize` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `totalPages` | integer or string (int32) | No | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `hasPreviousPage` | boolean | No | — |
| `hasNextPage` | boolean | No | — |

## PagedResultOfCatalogLocationDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `items` | array of CatalogLocationDto | Yes | — |
| `totalCount` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `pageNumber` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `pageSize` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `totalPages` | integer or string (int32) | No | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `hasPreviousPage` | boolean | No | — |
| `hasNextPage` | boolean | No | — |

## PagedResultOfCatalogProductDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `items` | array of CatalogProductDto | Yes | — |
| `totalCount` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `pageNumber` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `pageSize` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `totalPages` | integer or string (int32) | No | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `hasPreviousPage` | boolean | No | — |
| `hasNextPage` | boolean | No | — |

## PagedResultOfCatalogStockDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `items` | array of CatalogStockDto | Yes | — |
| `totalCount` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `pageNumber` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `pageSize` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `totalPages` | integer or string (int32) | No | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `hasPreviousPage` | boolean | No | — |
| `hasNextPage` | boolean | No | — |

## PagedResultOfExpenseDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `items` | array of ExpenseDto | Yes | — |
| `totalCount` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `pageNumber` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `pageSize` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `totalPages` | integer or string (int32) | No | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `hasPreviousPage` | boolean | No | — |
| `hasNextPage` | boolean | No | — |

## PagedResultOfInventoryLocationDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `items` | array of InventoryLocationDto | Yes | — |
| `totalCount` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `pageNumber` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `pageSize` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `totalPages` | integer or string (int32) | No | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `hasPreviousPage` | boolean | No | — |
| `hasNextPage` | boolean | No | — |

## PagedResultOfProductDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `items` | array of ProductDto | Yes | — |
| `totalCount` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `pageNumber` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `pageSize` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `totalPages` | integer or string (int32) | No | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `hasPreviousPage` | boolean | No | — |
| `hasNextPage` | boolean | No | — |

## PagedResultOfPurchaseInvoiceDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `items` | array of PurchaseInvoiceDto | Yes | — |
| `totalCount` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `pageNumber` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `pageSize` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `totalPages` | integer or string (int32) | No | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `hasPreviousPage` | boolean | No | — |
| `hasNextPage` | boolean | No | — |

## PagedResultOfReturnDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `items` | array of ReturnDto | Yes | — |
| `totalCount` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `pageNumber` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `pageSize` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `totalPages` | integer or string (int32) | No | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `hasPreviousPage` | boolean | No | — |
| `hasNextPage` | boolean | No | — |

## PagedResultOfSaleDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `items` | array of SaleDto | Yes | — |
| `totalCount` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `pageNumber` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `pageSize` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `totalPages` | integer or string (int32) | No | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `hasPreviousPage` | boolean | No | — |
| `hasNextPage` | boolean | No | — |

## PagedResultOfStockBalanceDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `items` | array of StockBalanceDto | Yes | — |
| `totalCount` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `pageNumber` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `pageSize` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `totalPages` | integer or string (int32) | No | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `hasPreviousPage` | boolean | No | — |
| `hasNextPage` | boolean | No | — |

## PagedResultOfStockCostDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `items` | array of StockCostDto | Yes | — |
| `totalCount` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `pageNumber` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `pageSize` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `totalPages` | integer or string (int32) | No | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `hasPreviousPage` | boolean | No | — |
| `hasNextPage` | boolean | No | — |

## PagedResultOfStockMovementDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `items` | array of StockMovementDto | Yes | — |
| `totalCount` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `pageNumber` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `pageSize` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `totalPages` | integer or string (int32) | No | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `hasPreviousPage` | boolean | No | — |
| `hasNextPage` | boolean | No | — |

## PagedResultOfStockTransferDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `items` | array of StockTransferDto | Yes | — |
| `totalCount` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `pageNumber` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `pageSize` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `totalPages` | integer or string (int32) | No | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `hasPreviousPage` | boolean | No | — |
| `hasNextPage` | boolean | No | — |

## PagedResultOfSupplierDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `items` | array of SupplierDto | Yes | — |
| `totalCount` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `pageNumber` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `pageSize` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `totalPages` | integer or string (int32) | No | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `hasPreviousPage` | boolean | No | — |
| `hasNextPage` | boolean | No | — |

## PagedResultOfUserDetailsDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `items` | array of UserDetailsDto | Yes | — |
| `totalCount` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `pageNumber` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `pageSize` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `totalPages` | integer or string (int32) | No | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `hasPreviousPage` | boolean | No | — |
| `hasNextPage` | boolean | No | — |

## ProblemDetails

| Field | Type | Required | Constraints |
|---|---|---|---|
| `type` | null or string | No | — |
| `title` | null or string | No | — |
| `status` | null or integer or string (int32) | No | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `detail` | null or string | No | — |
| `instance` | null or string | No | — |

## ProductDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `id` | string (uuid) | Yes | — |
| `name` | string | Yes | — |
| `sku` | string | Yes | — |
| `isActive` | boolean | Yes | — |
| `categoryId` | string (uuid) | Yes | — |
| `categoryName` | string | Yes | — |
| `photos` | array of ProductPhotoDto | No | — |

## ProductPhotoDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `id` | string (uuid) | Yes | — |
| `storageKey` | string | Yes | — |
| `contentType` | string | Yes | — |
| `fileSizeBytes` | integer or string (int64) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `contentHash` | string | Yes | — |
| `sortOrder` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `createdAtUtc` | string (date-time) | Yes | — |
| `downloadPath` | string | No | — |

## PurchaseInvoiceDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `id` | string (uuid) | Yes | — |
| `number` | string | Yes | — |
| `supplierInvoiceNumber` | null or string | Yes | — |
| `supplierId` | string (uuid) | Yes | — |
| `inventoryLocationId` | string (uuid) | Yes | — |
| `invoiceDate` | string (date) | Yes | — |
| `status` | string | Yes | — |
| `notes` | null or string | Yes | — |
| `createdAtUtc` | string (date-time) | Yes | — |
| `postedAtUtc` | null or string (date-time) | Yes | — |
| `totalAmount` | number or string (double) | Yes | pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |
| `lines` | array of PurchaseInvoiceLineDto | Yes | — |

## PurchaseInvoiceLineDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `id` | string (uuid) | Yes | — |
| `lineNumber` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `productId` | string (uuid) | Yes | — |
| `quantity` | number or string (double) | Yes | pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |
| `unitCost` | number or string (double) | Yes | pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |
| `lineAmount` | number or string (double) | Yes | pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |

## ReconcileSaleDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `originalSellerUserId` | string (uuid) | No | — |
| `submission` | SyncSaleDto | Yes | — |

## RecordHistoricalCostDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `purchaseUnitCost` | null or number or string (double) | Yes | minimum: 0; maximum: 1000000000; pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |
| `reason` | string | Yes | minLength: 3; maxLength: 1000 |

## RefreshRequestDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `refreshToken` | string | Yes | minLength: 0; maxLength: 256 |

## RequestStockTransferReturnDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `reason` | string | Yes | minLength: 0; maxLength: 500 |

## ResetPasswordRequestDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `userId` | string (uuid) | No | — |
| `token` | string | Yes | minLength: 0; maxLength: 4096 |
| `newPassword` | string | Yes | minLength: 4; maxLength: 1024 |
| `confirmPassword` | string | Yes | minLength: 0; maxLength: 1024 |

## ReturnDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `id` | string (uuid) | Yes | — |
| `number` | string | Yes | — |
| `clientReturnId` | string (uuid) | Yes | — |
| `salesInvoiceId` | string (uuid) | Yes | — |
| `createdByUserId` | string (uuid) | Yes | — |
| `createdAtUtc` | string (date-time) | Yes | — |
| `reason` | string | Yes | — |
| `refund` | number or string (double) | Yes | pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |
| `lines` | array of ReturnLineDto | Yes | — |

## ReturnLineDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `salesInvoiceLineId` | string (uuid) | Yes | — |
| `quantity` | number or string (double) | Yes | pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |
| `restock` | boolean | Yes | — |
| `refund` | number or string (double) | Yes | pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |

## SaleAccountingLineDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `id` | string (uuid) | Yes | — |
| `productId` | string (uuid) | Yes | — |
| `quantity` | number or string (double) | Yes | pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |
| `returnedQuantity` | number or string (double) | Yes | pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |
| `purchaseUnitCost` | null or number or string (double) | Yes | pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |
| `baselineUnitPrice` | number or string (double) | Yes | pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |
| `sellingUnitPrice` | number or string (double) | Yes | pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |
| `stakeholderProfit` | null or number or string (double) | Yes | pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |
| `branchGrossProfit` | number or string (double) | Yes | pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |
| `costRecordedByUserId` | null or string (uuid) | Yes | — |
| `costRecordedAtUtc` | null or string (date-time) | Yes | — |
| `costReason` | null or string | Yes | — |

## SaleDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `id` | string (uuid) | Yes | — |
| `number` | string | Yes | — |
| `clientSaleId` | string (uuid) | Yes | — |
| `branchId` | string (uuid) | Yes | — |
| `inventoryLocationId` | string (uuid) | Yes | — |
| `soldByUserId` | string (uuid) | Yes | — |
| `soldAtUtc` | string (date-time) | Yes | — |
| `createdAtUtc` | string (date-time) | Yes | — |
| `notes` | null or string | Yes | — |
| `total` | number or string (double) | Yes | pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |
| `lines` | array of SaleLineDto | Yes | — |
| `isOffline` | boolean | No | — |
| `reconciledByUserId` | null or string (uuid) | No | — |
| `reconciledAtUtc` | null or string (date-time) | No | — |

## SaleLineDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `id` | string (uuid) | Yes | — |
| `productId` | string (uuid) | Yes | — |
| `productName` | string | Yes | — |
| `sku` | string | Yes | — |
| `quantity` | number or string (double) | Yes | pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |
| `sellingUnitPrice` | number or string (double) | Yes | pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |
| `total` | number or string (double) | Yes | pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |
| `returnedQuantity` | number or string (double) | No | pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |

## SaveBranchDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `name` | string | Yes | minLength: 0; maxLength: 100 |
| `code` | string | Yes | minLength: 0; maxLength: 32 |
| `address` | null or string | No | minLength: 0; maxLength: 300 |
| `phone` | null or string | No | minLength: 0; maxLength: 30 |
| `isActive` | boolean | No | — |

## SaveCategoryDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `name` | string | Yes | minLength: 0; maxLength: 100 |

## SaveInventoryLocationDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `name` | string | Yes | minLength: 0; maxLength: 100 |
| `code` | string | Yes | minLength: 0; maxLength: 32 |
| `isActive` | boolean | No | — |

## SaveProductDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `name` | string | Yes | minLength: 0; maxLength: 200 |
| `sku` | string | Yes | minLength: 0; maxLength: 64 |
| `categoryId` | string (uuid) | No | — |
| `isActive` | boolean | No | — |

## SaveSupplierDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `name` | string | Yes | minLength: 0; maxLength: 150 |
| `code` | string | Yes | minLength: 0; maxLength: 32 |
| `phone` | null or string | No | minLength: 0; maxLength: 30 |
| `email` | null or string | No | minLength: 0; maxLength: 254 |
| `address` | null or string | No | minLength: 0; maxLength: 300 |
| `isActive` | boolean | No | — |

## SellerProductPriceDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `branchId` | string (uuid) | Yes | — |
| `productId` | string (uuid) | Yes | — |
| `minimumSellingPrice` | number or string (double) | Yes | pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |
| `priceRevision` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `updatedAtUtc` | string (date-time) | Yes | — |

## StakeholderBranchProfitDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `branchId` | string (uuid) | Yes | — |
| `branchName` | string | Yes | — |
| `baselineSalesValue` | number or string (double) | Yes | pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |
| `returnedBaselineValue` | number or string (double) | Yes | pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |
| `netBaselineValue` | number or string (double) | Yes | pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |
| `netPurchaseCost` | null or number or string (double) | Yes | pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |
| `grossProfit` | null or number or string (double) | Yes | pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |
| `missingCostEntries` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |

## StakeholderProfitDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `fromUtc` | string (date-time) | Yes | — |
| `toUtc` | string (date-time) | Yes | — |
| `branchId` | null or string (uuid) | Yes | — |
| `netBaselineValue` | number or string (double) | Yes | pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |
| `netPurchaseCost` | null or number or string (double) | Yes | pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |
| `grossProfit` | null or number or string (double) | Yes | pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |
| `missingCostEntries` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `branches` | array of StakeholderBranchProfitDto | Yes | — |

## StockBalanceDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `id` | string (uuid) | Yes | — |
| `productId` | string (uuid) | Yes | — |
| `productName` | string | Yes | — |
| `sku` | string | Yes | — |
| `inventoryLocationId` | string (uuid) | Yes | — |
| `inventoryLocationName` | string | Yes | — |
| `inventoryLocationCode` | string | Yes | — |
| `quantity` | number or string (double) | Yes | pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |

## StockCostDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `productId` | string (uuid) | Yes | — |
| `inventoryLocationId` | string (uuid) | Yes | — |
| `quantity` | number or string (double) | Yes | pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |
| `averagePurchaseUnitCost` | null or number or string (double) | Yes | pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |

## StockMovementDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `id` | string (uuid) | Yes | — |
| `productId` | string (uuid) | Yes | — |
| `productName` | string | Yes | — |
| `sku` | string | Yes | — |
| `inventoryLocationId` | string (uuid) | Yes | — |
| `inventoryLocationName` | string | Yes | — |
| `movementType` | string | Yes | — |
| `quantityChange` | number or string (double) | Yes | pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |
| `occurredAtUtc` | string (date-time) | Yes | — |
| `purchaseInvoiceId` | null or string (uuid) | Yes | — |
| `purchaseInvoiceLineId` | null or string (uuid) | Yes | — |
| `stockTransferId` | null or string (uuid) | Yes | — |
| `stockTransferLineId` | null or string (uuid) | Yes | — |
| `salesInvoiceLineId` | null or string (uuid) | No | — |
| `salesReturnLineId` | null or string (uuid) | No | — |
| `stockAdjustmentId` | null or string (uuid) | No | — |

## StockTransferDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `id` | string (uuid) | Yes | — |
| `number` | string | Yes | — |
| `sourceLocationId` | string (uuid) | Yes | — |
| `destinationLocationId` | string (uuid) | Yes | — |
| `status` | string | Yes | — |
| `notes` | null or string | Yes | — |
| `createdAtUtc` | string (date-time) | Yes | — |
| `shippedAtUtc` | null or string (date-time) | Yes | — |
| `receivedAtUtc` | null or string (date-time) | Yes | — |
| `returnReason` | null or string | Yes | — |
| `returnRequestedAtUtc` | null or string (date-time) | Yes | — |
| `returnedAtUtc` | null or string (date-time) | Yes | — |
| `totalBaselineAmount` | number or string (double) | Yes | pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |
| `lines` | array of StockTransferLineDto | Yes | — |

## StockTransferLineDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `id` | string (uuid) | Yes | — |
| `lineNumber` | integer or string (int32) | Yes | pattern: ^-?(?:0\|[1-9]\d*)$ |
| `productId` | string (uuid) | Yes | — |
| `quantity` | number or string (double) | Yes | pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |
| `baselineUnitPrice` | number or string (double) | Yes | pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |
| `baselineAmount` | number or string (double) | Yes | pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |

## SupplierDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `id` | string (uuid) | Yes | — |
| `name` | string | Yes | — |
| `code` | string | Yes | — |
| `phone` | null or string | Yes | — |
| `email` | null or string | Yes | — |
| `address` | null or string | Yes | — |
| `isActive` | boolean | Yes | — |

## SyncSaleDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `soldAtUtc` | string (date-time) | No | — |
| `sale` | CreateSaleDto | Yes | — |

## UpdateBranchPriceDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `price` | number or string (double) | No | minimum: 0; maximum: 1000000000; pattern: ^-?(?:0\|[1-9]\d*)(?:\.\d+)?$ |
| `expectedRevision` | integer or string (int32) | No | minimum: 0; maximum: 2147483647; pattern: ^-?(?:0\|[1-9]\d*)$ |
| `reason` | string | Yes | minLength: 0; maxLength: 500 |

## UpdateProductPhotoOrderRequest

| Field | Type | Required | Constraints |
|---|---|---|---|
| `sortOrder` | null or integer or string (int32) | Yes | minimum: 0; maximum: 2147483647; pattern: ^-?(?:0\|[1-9]\d*)$ |

## UpdateUserAccessDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `isActive` | boolean | No | — |
| `branchIds` | array of string (uuid) | Yes | maxItems: 100 |

## UserDetailsDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `id` | string (uuid) | Yes | — |
| `userName` | string | Yes | — |
| `fullName` | string | Yes | — |
| `email` | string | Yes | — |
| `isActive` | boolean | Yes | — |
| `emailConfirmed` | boolean | Yes | — |
| `createdAtUtc` | string (date-time) | Yes | — |
| `roles` | array of string | Yes | — |
| `branchIds` | array of string (uuid) | Yes | — |

## VoidExpenseDto

| Field | Type | Required | Constraints |
|---|---|---|---|
| `reason` | string | Yes | minLength: 3; maxLength: 1000 |
