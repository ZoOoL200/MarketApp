START TRANSACTION;
ALTER TABLE "StockMovements" DROP CONSTRAINT "CK_StockMovements_Source";

ALTER TABLE "StockMovements" DROP CONSTRAINT "CK_StockMovements_TypeAndQuantity";

ALTER TABLE "SalesInvoiceLines" DROP CONSTRAINT "CK_SalesLines_Quantity";

ALTER TABLE "StockMovements" ADD "SalesReturnLineId" uuid;

ALTER TABLE "StockMovements" ADD "StockAdjustmentId" uuid;

ALTER TABLE "SalesInvoices" ADD "IsOffline" boolean NOT NULL DEFAULT FALSE;

ALTER TABLE "SalesInvoices" ADD "ReconciledAtUtc" timestamp with time zone;

ALTER TABLE "SalesInvoices" ADD "ReconciledByUserId" uuid;

ALTER TABLE "SalesInvoiceLines" ADD "ReturnedQuantity" numeric(18,3) NOT NULL DEFAULT 0.0;

CREATE TABLE "SalesReturns" (
    "Id" uuid NOT NULL,
    "Number" character varying(40) NOT NULL,
    "ClientReturnId" uuid NOT NULL,
    "RequestHash" character varying(64) NOT NULL,
    "SalesInvoiceId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "CreatedByUserId" uuid NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "Reason" character varying(1000) NOT NULL,
    CONSTRAINT "PK_SalesReturns" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_SalesReturns_AspNetUsers_CreatedByUserId" FOREIGN KEY ("CreatedByUserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_SalesReturns_Branches_BranchId" FOREIGN KEY ("BranchId") REFERENCES "Branches" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_SalesReturns_SalesInvoices_SalesInvoiceId" FOREIGN KEY ("SalesInvoiceId") REFERENCES "SalesInvoices" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "StockAdjustments" (
    "Id" uuid NOT NULL,
    "ClientAdjustmentId" uuid NOT NULL,
    "RequestHash" character varying(64) NOT NULL,
    "ProductId" uuid NOT NULL,
    "InventoryLocationId" uuid NOT NULL,
    "QuantityChange" numeric(18,3) NOT NULL,
    "CreatedByUserId" uuid NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "Reason" character varying(1000) NOT NULL,
    CONSTRAINT "PK_StockAdjustments" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_Adjustments_Quantity" CHECK ("QuantityChange" <> 0),
    CONSTRAINT "FK_StockAdjustments_AspNetUsers_CreatedByUserId" FOREIGN KEY ("CreatedByUserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_StockAdjustments_InventoryLocations_InventoryLocationId" FOREIGN KEY ("InventoryLocationId") REFERENCES "InventoryLocations" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_StockAdjustments_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "SalesReturnLines" (
    "Id" uuid NOT NULL,
    "SalesReturnId" uuid NOT NULL,
    "SalesInvoiceLineId" uuid NOT NULL,
    "Quantity" numeric(18,3) NOT NULL,
    "Restock" boolean NOT NULL,
    CONSTRAINT "PK_SalesReturnLines" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_ReturnLines_Quantity" CHECK ("Quantity" > 0),
    CONSTRAINT "FK_SalesReturnLines_SalesInvoiceLines_SalesInvoiceLineId" FOREIGN KEY ("SalesInvoiceLineId") REFERENCES "SalesInvoiceLines" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_SalesReturnLines_SalesReturns_SalesReturnId" FOREIGN KEY ("SalesReturnId") REFERENCES "SalesReturns" ("Id") ON DELETE RESTRICT
);

CREATE UNIQUE INDEX "IX_StockMovements_SalesReturnLineId" ON "StockMovements" ("SalesReturnLineId") WHERE "SalesReturnLineId" IS NOT NULL;

CREATE UNIQUE INDEX "IX_StockMovements_StockAdjustmentId" ON "StockMovements" ("StockAdjustmentId") WHERE "StockAdjustmentId" IS NOT NULL;

ALTER TABLE "StockMovements" ADD CONSTRAINT "CK_StockMovements_Source" CHECK (num_nonnulls("PurchaseInvoiceLineId", "StockTransferLineId", "SalesInvoiceLineId", "SalesReturnLineId", "StockAdjustmentId") = 1 AND (
("MovementType" = 1 AND "PurchaseInvoiceLineId" IS NOT NULL)
OR ("MovementType" IN (2, 3, 4) AND "StockTransferLineId" IS NOT NULL)
OR ("MovementType" = 5 AND "SalesInvoiceLineId" IS NOT NULL)
OR ("MovementType" = 6 AND "SalesReturnLineId" IS NOT NULL)
OR ("MovementType" = 7 AND "StockAdjustmentId" IS NOT NULL)));

ALTER TABLE "StockMovements" ADD CONSTRAINT "CK_StockMovements_TypeAndQuantity" CHECK (("MovementType" IN (1, 3, 4, 6) AND "QuantityChange" > 0) OR ("MovementType" IN (2, 5) AND "QuantityChange" < 0) OR ("MovementType" = 7 AND "QuantityChange" <> 0));

CREATE INDEX "IX_SalesInvoices_ReconciledByUserId" ON "SalesInvoices" ("ReconciledByUserId");

ALTER TABLE "SalesInvoices" ADD CONSTRAINT "CK_SalesInvoices_Reconciliation" CHECK (("ReconciledByUserId" IS NULL AND "ReconciledAtUtc" IS NULL) OR
("ReconciledByUserId" IS NOT NULL AND "ReconciledAtUtc" IS NOT NULL AND "IsOffline"));

ALTER TABLE "SalesInvoiceLines" ADD CONSTRAINT "CK_SalesLines_Quantity" CHECK ("Quantity" > 0 AND "ReturnedQuantity" >= 0 AND "ReturnedQuantity" <= "Quantity");

CREATE INDEX "IX_SalesReturnLines_SalesInvoiceLineId" ON "SalesReturnLines" ("SalesInvoiceLineId");

CREATE UNIQUE INDEX "IX_SalesReturnLines_SalesReturnId_SalesInvoiceLineId" ON "SalesReturnLines" ("SalesReturnId", "SalesInvoiceLineId");

CREATE UNIQUE INDEX "IX_SalesReturns_BranchId_ClientReturnId" ON "SalesReturns" ("BranchId", "ClientReturnId");

CREATE INDEX "IX_SalesReturns_BranchId_CreatedAtUtc_Id" ON "SalesReturns" ("BranchId", "CreatedAtUtc", "Id");

CREATE INDEX "IX_SalesReturns_CreatedByUserId" ON "SalesReturns" ("CreatedByUserId");

CREATE UNIQUE INDEX "IX_SalesReturns_Number" ON "SalesReturns" ("Number");

CREATE INDEX "IX_SalesReturns_SalesInvoiceId" ON "SalesReturns" ("SalesInvoiceId");

CREATE UNIQUE INDEX "IX_StockAdjustments_ClientAdjustmentId" ON "StockAdjustments" ("ClientAdjustmentId");

CREATE INDEX "IX_StockAdjustments_CreatedAtUtc_Id" ON "StockAdjustments" ("CreatedAtUtc", "Id");

CREATE INDEX "IX_StockAdjustments_CreatedByUserId" ON "StockAdjustments" ("CreatedByUserId");

CREATE INDEX "IX_StockAdjustments_InventoryLocationId" ON "StockAdjustments" ("InventoryLocationId");

CREATE INDEX "IX_StockAdjustments_ProductId" ON "StockAdjustments" ("ProductId");

ALTER TABLE "SalesInvoices" ADD CONSTRAINT "FK_SalesInvoices_AspNetUsers_ReconciledByUserId" FOREIGN KEY ("ReconciledByUserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT;

ALTER TABLE "StockMovements" ADD CONSTRAINT "FK_StockMovements_SalesReturnLines_SalesReturnLineId" FOREIGN KEY ("SalesReturnLineId") REFERENCES "SalesReturnLines" ("Id") ON DELETE RESTRICT;

ALTER TABLE "StockMovements" ADD CONSTRAINT "FK_StockMovements_StockAdjustments_StockAdjustmentId" FOREIGN KEY ("StockAdjustmentId") REFERENCES "StockAdjustments" ("Id") ON DELETE RESTRICT;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261008114452_AddReturnsSyncAndReports', '10.0.12');

COMMIT;
