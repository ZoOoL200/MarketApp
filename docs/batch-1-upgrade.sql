START TRANSACTION;
ALTER TABLE "StockMovements" DROP CONSTRAINT "CK_StockMovements_Source";

ALTER TABLE "StockMovements" DROP CONSTRAINT "CK_StockMovements_TypeAndQuantity";

ALTER TABLE "StockMovements" ADD "SalesInvoiceLineId" uuid;

CREATE TABLE "SalesInvoices" (
    "Id" uuid NOT NULL,
    "Number" character varying(40) NOT NULL,
    "ClientSaleId" uuid NOT NULL,
    "RequestHash" character varying(64) NOT NULL,
    "BranchId" uuid NOT NULL,
    "InventoryLocationId" uuid NOT NULL,
    "SoldByUserId" uuid NOT NULL,
    "SoldAtUtc" timestamp with time zone NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "Notes" character varying(1000),
    CONSTRAINT "PK_SalesInvoices" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_SalesInvoices_AspNetUsers_SoldByUserId" FOREIGN KEY ("SoldByUserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_SalesInvoices_Branches_BranchId" FOREIGN KEY ("BranchId") REFERENCES "Branches" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_SalesInvoices_InventoryLocations_InventoryLocationId" FOREIGN KEY ("InventoryLocationId") REFERENCES "InventoryLocations" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "SalesInvoiceLines" (
    "Id" uuid NOT NULL,
    "SalesInvoiceId" uuid NOT NULL,
    "LineNumber" integer NOT NULL,
    "ProductId" uuid NOT NULL,
    "ProductNameSnapshot" character varying(200) NOT NULL,
    "ProductSkuSnapshot" character varying(64) NOT NULL,
    "Quantity" numeric(18,3) NOT NULL,
    "BaselineUnitPriceSnapshot" numeric(18,4) NOT NULL,
    "MinimumSellingPriceSnapshot" numeric(18,4) NOT NULL,
    "SellingUnitPrice" numeric(18,4) NOT NULL,
    "PriceRevisionSnapshot" integer NOT NULL,
    CONSTRAINT "PK_SalesInvoiceLines" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_SalesLines_Price" CHECK ("BaselineUnitPriceSnapshot" >= 0 AND "MinimumSellingPriceSnapshot" >= 0 AND "SellingUnitPrice" >= "MinimumSellingPriceSnapshot"),
    CONSTRAINT "CK_SalesLines_Quantity" CHECK ("Quantity" > 0),
    CONSTRAINT "CK_SalesLines_Sequence" CHECK ("LineNumber" > 0 AND "PriceRevisionSnapshot" > 0),
    CONSTRAINT "FK_SalesInvoiceLines_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_SalesInvoiceLines_SalesInvoices_SalesInvoiceId" FOREIGN KEY ("SalesInvoiceId") REFERENCES "SalesInvoices" ("Id") ON DELETE RESTRICT
);

CREATE UNIQUE INDEX "UX_StockMovements_SalesInvoiceLine" ON "StockMovements" ("SalesInvoiceLineId") WHERE "SalesInvoiceLineId" IS NOT NULL;

ALTER TABLE "StockMovements" ADD CONSTRAINT "CK_StockMovements_Source" CHECK (("MovementType" = 1 AND "PurchaseInvoiceLineId" IS NOT NULL
    AND "StockTransferLineId" IS NULL AND "SalesInvoiceLineId" IS NULL)
OR ("MovementType" IN (2, 3, 4) AND "StockTransferLineId" IS NOT NULL
    AND "PurchaseInvoiceLineId" IS NULL AND "SalesInvoiceLineId" IS NULL)
OR ("MovementType" = 5 AND "SalesInvoiceLineId" IS NOT NULL
    AND "PurchaseInvoiceLineId" IS NULL AND "StockTransferLineId" IS NULL));

ALTER TABLE "StockMovements" ADD CONSTRAINT "CK_StockMovements_TypeAndQuantity" CHECK (("MovementType" IN (1, 3, 4) AND "QuantityChange" > 0) OR ("MovementType" IN (2, 5) AND "QuantityChange" < 0));

CREATE INDEX "IX_SalesInvoiceLines_ProductId" ON "SalesInvoiceLines" ("ProductId");

CREATE UNIQUE INDEX "IX_SalesInvoiceLines_SalesInvoiceId_LineNumber" ON "SalesInvoiceLines" ("SalesInvoiceId", "LineNumber");

CREATE UNIQUE INDEX "IX_SalesInvoiceLines_SalesInvoiceId_ProductId" ON "SalesInvoiceLines" ("SalesInvoiceId", "ProductId");

CREATE UNIQUE INDEX "IX_SalesInvoices_BranchId_ClientSaleId" ON "SalesInvoices" ("BranchId", "ClientSaleId");

CREATE INDEX "IX_SalesInvoices_BranchId_SoldAtUtc_Id" ON "SalesInvoices" ("BranchId", "SoldAtUtc", "Id");

CREATE INDEX "IX_SalesInvoices_InventoryLocationId" ON "SalesInvoices" ("InventoryLocationId");

CREATE UNIQUE INDEX "IX_SalesInvoices_Number" ON "SalesInvoices" ("Number");

CREATE INDEX "IX_SalesInvoices_SoldByUserId" ON "SalesInvoices" ("SoldByUserId");

ALTER TABLE "StockMovements" ADD CONSTRAINT "FK_StockMovements_SalesInvoiceLines_SalesInvoiceLineId" FOREIGN KEY ("SalesInvoiceLineId") REFERENCES "SalesInvoiceLines" ("Id") ON DELETE RESTRICT;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261008053608_AddSalesInvoices', '10.0.12');

COMMIT;

