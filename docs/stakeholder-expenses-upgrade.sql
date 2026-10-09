START TRANSACTION;
CREATE TABLE "StakeholderExpenses" (
    "Id" uuid NOT NULL,
    "ClientExpenseId" uuid NOT NULL,
    "RequestHash" character varying(64) NOT NULL,
    "BranchId" uuid,
    "Category" character varying(20) NOT NULL,
    "Amount" numeric(18,4) NOT NULL,
    "OccurredAtUtc" timestamp with time zone NOT NULL,
    "Description" character varying(1000) NOT NULL,
    "PaidTo" character varying(200),
    "ReferenceNumber" character varying(100),
    "CreatedByUserId" uuid NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "VoidedByUserId" uuid,
    "VoidedAtUtc" timestamp with time zone,
    "VoidReason" character varying(1000),
    CONSTRAINT "PK_StakeholderExpenses" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_StakeholderExpenses_Amount" CHECK ("Amount" > 0),
    CONSTRAINT "CK_StakeholderExpenses_Category" CHECK ("Category" IN ('Rent', 'Other')),
    CONSTRAINT "CK_StakeholderExpenses_Void" CHECK (("VoidedAtUtc" IS NULL AND "VoidedByUserId" IS NULL AND "VoidReason" IS NULL) OR
("VoidedAtUtc" IS NOT NULL AND "VoidedByUserId" IS NOT NULL AND "VoidReason" IS NOT NULL)),
    CONSTRAINT "FK_StakeholderExpenses_AspNetUsers_CreatedByUserId" FOREIGN KEY ("CreatedByUserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_StakeholderExpenses_AspNetUsers_VoidedByUserId" FOREIGN KEY ("VoidedByUserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_StakeholderExpenses_Branches_BranchId" FOREIGN KEY ("BranchId") REFERENCES "Branches" ("Id") ON DELETE RESTRICT
);

CREATE INDEX "IX_StakeholderExpenses_BranchId_OccurredAtUtc_Id" ON "StakeholderExpenses" ("BranchId", "OccurredAtUtc", "Id");

CREATE UNIQUE INDEX "IX_StakeholderExpenses_ClientExpenseId" ON "StakeholderExpenses" ("ClientExpenseId");

CREATE INDEX "IX_StakeholderExpenses_CreatedByUserId" ON "StakeholderExpenses" ("CreatedByUserId");

CREATE INDEX "IX_StakeholderExpenses_OccurredAtUtc_Id" ON "StakeholderExpenses" ("OccurredAtUtc", "Id");

CREATE INDEX "IX_StakeholderExpenses_VoidedByUserId" ON "StakeholderExpenses" ("VoidedByUserId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261009155617_AddStakeholderExpenseRegister', '10.0.12');

COMMIT;
