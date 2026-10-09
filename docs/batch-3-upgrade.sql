START TRANSACTION;
ALTER TABLE "SalesInvoices" ADD "ManagerSharePercentSnapshot" numeric(7,4) NOT NULL DEFAULT 0.0;

ALTER TABLE "SalesInvoices" ADD "ManagerUserIdSnapshot" uuid;

CREATE TABLE "ManagerProfitShares" (
    "Id" uuid NOT NULL,
    "ClientChangeId" uuid NOT NULL,
    "RequestHash" character varying(64) NOT NULL,
    "Reason" character varying(1000) NOT NULL,
    "BranchId" uuid NOT NULL,
    "ManagerUserId" uuid NOT NULL,
    "Percent" numeric(7,4) NOT NULL,
    "EffectiveFromUtc" timestamp with time zone NOT NULL,
    "CreatedByUserId" uuid NOT NULL,
    CONSTRAINT "PK_ManagerProfitShares" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_ManagerShare_Percent" CHECK ("Percent" BETWEEN 0 AND 100),
    CONSTRAINT "FK_ManagerProfitShares_AspNetUsers_CreatedByUserId" FOREIGN KEY ("CreatedByUserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_ManagerProfitShares_AspNetUsers_ManagerUserId" FOREIGN KEY ("ManagerUserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_ManagerProfitShares_Branches_BranchId" FOREIGN KEY ("BranchId") REFERENCES "Branches" ("Id") ON DELETE RESTRICT
);

CREATE INDEX "IX_SalesInvoices_ManagerUserIdSnapshot" ON "SalesInvoices" ("ManagerUserIdSnapshot");

ALTER TABLE "SalesInvoices" ADD CONSTRAINT "CK_SalesInvoices_ManagerShare" CHECK ("ManagerSharePercentSnapshot" BETWEEN 0 AND 100 AND
("ManagerUserIdSnapshot" IS NOT NULL OR "ManagerSharePercentSnapshot" = 0));

CREATE UNIQUE INDEX "IX_ManagerProfitShares_BranchId_ClientChangeId" ON "ManagerProfitShares" ("BranchId", "ClientChangeId");

CREATE UNIQUE INDEX "IX_ManagerProfitShares_BranchId_EffectiveFromUtc" ON "ManagerProfitShares" ("BranchId", "EffectiveFromUtc");

CREATE INDEX "IX_ManagerProfitShares_CreatedByUserId" ON "ManagerProfitShares" ("CreatedByUserId");

CREATE INDEX "IX_ManagerProfitShares_ManagerUserId" ON "ManagerProfitShares" ("ManagerUserId");

ALTER TABLE "SalesInvoices" ADD CONSTRAINT "FK_SalesInvoices_AspNetUsers_ManagerUserIdSnapshot" FOREIGN KEY ("ManagerUserIdSnapshot") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261008190152_AddManagerProfitShares', '10.0.12');

COMMIT;
