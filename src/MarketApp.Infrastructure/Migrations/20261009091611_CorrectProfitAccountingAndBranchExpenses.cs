using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CorrectProfitAccountingAndBranchExpenses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PurchaseUnitCostSnapshot",
                table: "StockTransferLines",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AveragePurchaseUnitCost",
                table: "StockBalances",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PurchaseCostReason",
                table: "SalesInvoiceLines",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PurchaseCostRecordHash",
                table: "SalesInvoiceLines",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PurchaseCostRecordedAtUtc",
                table: "SalesInvoiceLines",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PurchaseCostRecordedByUserId",
                table: "SalesInvoiceLines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PurchaseUnitCostSnapshot",
                table: "SalesInvoiceLines",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BranchExpenses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientExpenseId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EmployeeUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    VoidedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    VoidedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VoidReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BranchExpenses", x => x.Id);
                    table.CheckConstraint("CK_BranchExpense_Amount", "\"Amount\" > 0");
                    table.CheckConstraint("CK_BranchExpense_Category", "\"Category\" IN ('Salary', 'Other')");
                    table.CheckConstraint("CK_BranchExpense_Void", "(\"VoidedAtUtc\" IS NULL AND \"VoidedByUserId\" IS NULL AND \"VoidReason\" IS NULL) OR\n(\"VoidedAtUtc\" IS NOT NULL AND \"VoidedByUserId\" IS NOT NULL AND \"VoidReason\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_BranchExpenses_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BranchExpenses_AspNetUsers_EmployeeUserId",
                        column: x => x.EmployeeUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BranchExpenses_AspNetUsers_VoidedByUserId",
                        column: x => x.VoidedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BranchExpenses_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockCostHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryLocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AveragePurchaseUnitCost = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    QuantityAtChange = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    EffectiveAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    RecordedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ClientChangeId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockCostHistories", x => x.Id);
                    table.CheckConstraint("CK_StockCostHistory_Values", "\"AveragePurchaseUnitCost\" >= 0 AND \"QuantityAtChange\" >= 0");
                    table.ForeignKey(
                        name: "FK_StockCostHistories_AspNetUsers_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockCostHistories_InventoryLocations_InventoryLocationId",
                        column: x => x.InventoryLocationId,
                        principalTable: "InventoryLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockCostHistories_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_TransferCost_Nonnegative",
                table: "StockTransferLines",
                sql: "\"PurchaseUnitCostSnapshot\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BalanceCost_Nonnegative",
                table: "StockBalances",
                sql: "\"AveragePurchaseUnitCost\" >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoiceLines_PurchaseCostRecordedByUserId",
                table: "SalesInvoiceLines",
                column: "PurchaseCostRecordedByUserId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SaleCost_Nonnegative",
                table: "SalesInvoiceLines",
                sql: "\"PurchaseUnitCostSnapshot\" >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_BranchExpenses_BranchId_ClientExpenseId",
                table: "BranchExpenses",
                columns: new[] { "BranchId", "ClientExpenseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BranchExpenses_BranchId_OccurredAtUtc_Id",
                table: "BranchExpenses",
                columns: new[] { "BranchId", "OccurredAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_BranchExpenses_CreatedByUserId",
                table: "BranchExpenses",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BranchExpenses_EmployeeUserId",
                table: "BranchExpenses",
                column: "EmployeeUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BranchExpenses_VoidedByUserId",
                table: "BranchExpenses",
                column: "VoidedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StockCostHistories_ClientChangeId",
                table: "StockCostHistories",
                column: "ClientChangeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockCostHistories_InventoryLocationId",
                table: "StockCostHistories",
                column: "InventoryLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_StockCostHistories_ProductId_InventoryLocationId_EffectiveA~",
                table: "StockCostHistories",
                columns: new[] { "ProductId", "InventoryLocationId", "EffectiveAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_StockCostHistories_RecordedByUserId",
                table: "StockCostHistories",
                column: "RecordedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesInvoiceLines_AspNetUsers_PurchaseCostRecordedByUserId",
                table: "SalesInvoiceLines",
                column: "PurchaseCostRecordedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesInvoiceLines_AspNetUsers_PurchaseCostRecordedByUserId",
                table: "SalesInvoiceLines");

            migrationBuilder.DropTable(
                name: "BranchExpenses");

            migrationBuilder.DropTable(
                name: "StockCostHistories");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TransferCost_Nonnegative",
                table: "StockTransferLines");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BalanceCost_Nonnegative",
                table: "StockBalances");

            migrationBuilder.DropIndex(
                name: "IX_SalesInvoiceLines_PurchaseCostRecordedByUserId",
                table: "SalesInvoiceLines");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SaleCost_Nonnegative",
                table: "SalesInvoiceLines");

            migrationBuilder.DropColumn(
                name: "PurchaseUnitCostSnapshot",
                table: "StockTransferLines");

            migrationBuilder.DropColumn(
                name: "AveragePurchaseUnitCost",
                table: "StockBalances");

            migrationBuilder.DropColumn(
                name: "PurchaseCostReason",
                table: "SalesInvoiceLines");

            migrationBuilder.DropColumn(
                name: "PurchaseCostRecordHash",
                table: "SalesInvoiceLines");

            migrationBuilder.DropColumn(
                name: "PurchaseCostRecordedAtUtc",
                table: "SalesInvoiceLines");

            migrationBuilder.DropColumn(
                name: "PurchaseCostRecordedByUserId",
                table: "SalesInvoiceLines");

            migrationBuilder.DropColumn(
                name: "PurchaseUnitCostSnapshot",
                table: "SalesInvoiceLines");
        }
    }
}
