START TRANSACTION;
ALTER TABLE "StockTransferLines" ADD "PurchaseUnitCostSnapshot" numeric(18,6);

ALTER TABLE "StockBalances" ADD "AveragePurchaseUnitCost" numeric(18,6);

ALTER TABLE "SalesInvoiceLines" ADD "PurchaseCostReason" character varying(1000);

ALTER TABLE "SalesInvoiceLines" ADD "PurchaseCostRecordHash" character varying(64);

ALTER TABLE "SalesInvoiceLines" ADD "PurchaseCostRecordedAtUtc" timestamp with time zone;

ALTER TABLE "SalesInvoiceLines" ADD "PurchaseCostRecordedByUserId" uuid;

ALTER TABLE "SalesInvoiceLines" ADD "PurchaseUnitCostSnapshot" numeric(18,6);

CREATE TABLE "BranchExpenses" (
    "Id" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "ClientExpenseId" uuid NOT NULL,
    "RequestHash" character varying(64) NOT NULL,
    "Category" character varying(20) NOT NULL,
    "EmployeeUserId" uuid,
    "Amount" numeric(18,4) NOT NULL,
    "OccurredAtUtc" timestamp with time zone NOT NULL,
    "Description" character varying(1000) NOT NULL,
    "CreatedByUserId" uuid NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "VoidedByUserId" uuid,
    "VoidedAtUtc" timestamp with time zone,
    "VoidReason" character varying(1000),
    CONSTRAINT "PK_BranchExpenses" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_BranchExpense_Amount" CHECK ("Amount" > 0),
    CONSTRAINT "CK_BranchExpense_Category" CHECK ("Category" IN ('Salary', 'Other')),
    CONSTRAINT "CK_BranchExpense_Void" CHECK (("VoidedAtUtc" IS NULL AND "VoidedByUserId" IS NULL AND "VoidReason" IS NULL) OR
("VoidedAtUtc" IS NOT NULL AND "VoidedByUserId" IS NOT NULL AND "VoidReason" IS NOT NULL)),
    CONSTRAINT "FK_BranchExpenses_AspNetUsers_CreatedByUserId" FOREIGN KEY ("CreatedByUserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_BranchExpenses_AspNetUsers_EmployeeUserId" FOREIGN KEY ("EmployeeUserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_BranchExpenses_AspNetUsers_VoidedByUserId" FOREIGN KEY ("VoidedByUserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_BranchExpenses_Branches_BranchId" FOREIGN KEY ("BranchId") REFERENCES "Branches" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "StockCostHistories" (
    "Id" uuid NOT NULL,
    "ProductId" uuid NOT NULL,
    "InventoryLocationId" uuid NOT NULL,
    "AveragePurchaseUnitCost" numeric(18,6),
    "QuantityAtChange" numeric(18,3) NOT NULL,
    "EffectiveAtUtc" timestamp with time zone NOT NULL,
    "Reason" character varying(1000) NOT NULL,
    "RecordedByUserId" uuid,
    "ClientChangeId" uuid,
    "RequestHash" character varying(64),
    CONSTRAINT "PK_StockCostHistories" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_StockCostHistory_Values" CHECK ("AveragePurchaseUnitCost" >= 0 AND "QuantityAtChange" >= 0),
    CONSTRAINT "FK_StockCostHistories_AspNetUsers_RecordedByUserId" FOREIGN KEY ("RecordedByUserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_StockCostHistories_InventoryLocations_InventoryLocationId" FOREIGN KEY ("InventoryLocationId") REFERENCES "InventoryLocations" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_StockCostHistories_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE RESTRICT
);

ALTER TABLE "StockTransferLines" ADD CONSTRAINT "CK_TransferCost_Nonnegative" CHECK ("PurchaseUnitCostSnapshot" >= 0);

ALTER TABLE "StockBalances" ADD CONSTRAINT "CK_BalanceCost_Nonnegative" CHECK ("AveragePurchaseUnitCost" >= 0);

CREATE INDEX "IX_SalesInvoiceLines_PurchaseCostRecordedByUserId" ON "SalesInvoiceLines" ("PurchaseCostRecordedByUserId");

ALTER TABLE "SalesInvoiceLines" ADD CONSTRAINT "CK_SaleCost_Nonnegative" CHECK ("PurchaseUnitCostSnapshot" >= 0);

CREATE UNIQUE INDEX "IX_BranchExpenses_BranchId_ClientExpenseId" ON "BranchExpenses" ("BranchId", "ClientExpenseId");

CREATE INDEX "IX_BranchExpenses_BranchId_OccurredAtUtc_Id" ON "BranchExpenses" ("BranchId", "OccurredAtUtc", "Id");

CREATE INDEX "IX_BranchExpenses_CreatedByUserId" ON "BranchExpenses" ("CreatedByUserId");

CREATE INDEX "IX_BranchExpenses_EmployeeUserId" ON "BranchExpenses" ("EmployeeUserId");

CREATE INDEX "IX_BranchExpenses_VoidedByUserId" ON "BranchExpenses" ("VoidedByUserId");

CREATE UNIQUE INDEX "IX_StockCostHistories_ClientChangeId" ON "StockCostHistories" ("ClientChangeId");

CREATE INDEX "IX_StockCostHistories_InventoryLocationId" ON "StockCostHistories" ("InventoryLocationId");

CREATE INDEX "IX_StockCostHistories_ProductId_InventoryLocationId_EffectiveA~" ON "StockCostHistories" ("ProductId", "InventoryLocationId", "EffectiveAtUtc", "Id");

CREATE INDEX "IX_StockCostHistories_RecordedByUserId" ON "StockCostHistories" ("RecordedByUserId");

ALTER TABLE "SalesInvoiceLines" ADD CONSTRAINT "FK_SalesInvoiceLines_AspNetUsers_PurchaseCostRecordedByUserId" FOREIGN KEY ("PurchaseCostRecordedByUserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261009091611_CorrectProfitAccountingAndBranchExpenses', '10.0.12');

COMMIT;
