using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReturnsSyncAndReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_StockMovements_Source",
                table: "StockMovements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StockMovements_TypeAndQuantity",
                table: "StockMovements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SalesLines_Quantity",
                table: "SalesInvoiceLines");

            migrationBuilder.AddColumn<Guid>(
                name: "SalesReturnLineId",
                table: "StockMovements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StockAdjustmentId",
                table: "StockMovements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsOffline",
                table: "SalesInvoices",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReconciledAtUtc",
                table: "SalesInvoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReconciledByUserId",
                table: "SalesInvoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ReturnedQuantity",
                table: "SalesInvoiceLines",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "SalesInvoiceLines",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateTable(
                name: "SalesReturns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ClientReturnId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SalesInvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesReturns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesReturns_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesReturns_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesReturns_SalesInvoices_SalesInvoiceId",
                        column: x => x.SalesInvoiceId,
                        principalTable: "SalesInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockAdjustments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientAdjustmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryLocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuantityChange = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockAdjustments", x => x.Id);
                    table.CheckConstraint("CK_Adjustments_Quantity", "\"QuantityChange\" <> 0");
                    table.ForeignKey(
                        name: "FK_StockAdjustments_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockAdjustments_InventoryLocations_InventoryLocationId",
                        column: x => x.InventoryLocationId,
                        principalTable: "InventoryLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockAdjustments_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesReturnLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SalesReturnId = table.Column<Guid>(type: "uuid", nullable: false),
                    SalesInvoiceLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    Restock = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesReturnLines", x => x.Id);
                    table.CheckConstraint("CK_ReturnLines_Quantity", "\"Quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_SalesReturnLines_SalesInvoiceLines_SalesInvoiceLineId",
                        column: x => x.SalesInvoiceLineId,
                        principalTable: "SalesInvoiceLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesReturnLines_SalesReturns_SalesReturnId",
                        column: x => x.SalesReturnId,
                        principalTable: "SalesReturns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_SalesReturnLineId",
                table: "StockMovements",
                column: "SalesReturnLineId",
                unique: true,
                filter: "\"SalesReturnLineId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_StockAdjustmentId",
                table: "StockMovements",
                column: "StockAdjustmentId",
                unique: true,
                filter: "\"StockAdjustmentId\" IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockMovements_Source",
                table: "StockMovements",
                sql: "num_nonnulls(\"PurchaseInvoiceLineId\", \"StockTransferLineId\", \"SalesInvoiceLineId\", \"SalesReturnLineId\", \"StockAdjustmentId\") = 1 AND (\n(\"MovementType\" = 1 AND \"PurchaseInvoiceLineId\" IS NOT NULL)\nOR (\"MovementType\" IN (2, 3, 4) AND \"StockTransferLineId\" IS NOT NULL)\nOR (\"MovementType\" = 5 AND \"SalesInvoiceLineId\" IS NOT NULL)\nOR (\"MovementType\" = 6 AND \"SalesReturnLineId\" IS NOT NULL)\nOR (\"MovementType\" = 7 AND \"StockAdjustmentId\" IS NOT NULL))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockMovements_TypeAndQuantity",
                table: "StockMovements",
                sql: "(\"MovementType\" IN (1, 3, 4, 6) AND \"QuantityChange\" > 0) OR (\"MovementType\" IN (2, 5) AND \"QuantityChange\" < 0) OR (\"MovementType\" = 7 AND \"QuantityChange\" <> 0)");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_ReconciledByUserId",
                table: "SalesInvoices",
                column: "ReconciledByUserId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SalesInvoices_Reconciliation",
                table: "SalesInvoices",
                sql: "(\"ReconciledByUserId\" IS NULL AND \"ReconciledAtUtc\" IS NULL) OR\n(\"ReconciledByUserId\" IS NOT NULL AND \"ReconciledAtUtc\" IS NOT NULL AND \"IsOffline\")");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SalesLines_Quantity",
                table: "SalesInvoiceLines",
                sql: "\"Quantity\" > 0 AND \"ReturnedQuantity\" >= 0 AND \"ReturnedQuantity\" <= \"Quantity\"");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturnLines_SalesInvoiceLineId",
                table: "SalesReturnLines",
                column: "SalesInvoiceLineId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturnLines_SalesReturnId_SalesInvoiceLineId",
                table: "SalesReturnLines",
                columns: new[] { "SalesReturnId", "SalesInvoiceLineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturns_BranchId_ClientReturnId",
                table: "SalesReturns",
                columns: new[] { "BranchId", "ClientReturnId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturns_BranchId_CreatedAtUtc_Id",
                table: "SalesReturns",
                columns: new[] { "BranchId", "CreatedAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturns_CreatedByUserId",
                table: "SalesReturns",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturns_Number",
                table: "SalesReturns",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturns_SalesInvoiceId",
                table: "SalesReturns",
                column: "SalesInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_ClientAdjustmentId",
                table: "StockAdjustments",
                column: "ClientAdjustmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_CreatedAtUtc_Id",
                table: "StockAdjustments",
                columns: new[] { "CreatedAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_CreatedByUserId",
                table: "StockAdjustments",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_InventoryLocationId",
                table: "StockAdjustments",
                column: "InventoryLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_ProductId",
                table: "StockAdjustments",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesInvoices_AspNetUsers_ReconciledByUserId",
                table: "SalesInvoices",
                column: "ReconciledByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_SalesReturnLines_SalesReturnLineId",
                table: "StockMovements",
                column: "SalesReturnLineId",
                principalTable: "SalesReturnLines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_StockAdjustments_StockAdjustmentId",
                table: "StockMovements",
                column: "StockAdjustmentId",
                principalTable: "StockAdjustments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesInvoices_AspNetUsers_ReconciledByUserId",
                table: "SalesInvoices");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_SalesReturnLines_SalesReturnLineId",
                table: "StockMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_StockAdjustments_StockAdjustmentId",
                table: "StockMovements");

            migrationBuilder.DropTable(
                name: "SalesReturnLines");

            migrationBuilder.DropTable(
                name: "StockAdjustments");

            migrationBuilder.DropTable(
                name: "SalesReturns");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_SalesReturnLineId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_StockAdjustmentId",
                table: "StockMovements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StockMovements_Source",
                table: "StockMovements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StockMovements_TypeAndQuantity",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_SalesInvoices_ReconciledByUserId",
                table: "SalesInvoices");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SalesInvoices_Reconciliation",
                table: "SalesInvoices");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SalesLines_Quantity",
                table: "SalesInvoiceLines");

            migrationBuilder.DropColumn(
                name: "SalesReturnLineId",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "StockAdjustmentId",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "IsOffline",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "ReconciledAtUtc",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "ReconciledByUserId",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "ReturnedQuantity",
                table: "SalesInvoiceLines");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "SalesInvoiceLines");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockMovements_Source",
                table: "StockMovements",
                sql: "(\"MovementType\" = 1 AND \"PurchaseInvoiceLineId\" IS NOT NULL\n    AND \"StockTransferLineId\" IS NULL AND \"SalesInvoiceLineId\" IS NULL)\nOR (\"MovementType\" IN (2, 3, 4) AND \"StockTransferLineId\" IS NOT NULL\n    AND \"PurchaseInvoiceLineId\" IS NULL AND \"SalesInvoiceLineId\" IS NULL)\nOR (\"MovementType\" = 5 AND \"SalesInvoiceLineId\" IS NOT NULL\n    AND \"PurchaseInvoiceLineId\" IS NULL AND \"StockTransferLineId\" IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockMovements_TypeAndQuantity",
                table: "StockMovements",
                sql: "(\"MovementType\" IN (1, 3, 4) AND \"QuantityChange\" > 0) OR (\"MovementType\" IN (2, 5) AND \"QuantityChange\" < 0)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SalesLines_Quantity",
                table: "SalesInvoiceLines",
                sql: "\"Quantity\" > 0");
        }
    }
}
