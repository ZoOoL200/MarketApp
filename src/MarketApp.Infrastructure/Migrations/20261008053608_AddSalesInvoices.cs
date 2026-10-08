using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesInvoices : Migration
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

            migrationBuilder.AddColumn<Guid>(
                name: "SalesInvoiceLineId",
                table: "StockMovements",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SalesInvoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ClientSaleId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryLocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SoldByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SoldAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesInvoices_AspNetUsers_SoldByUserId",
                        column: x => x.SoldByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesInvoices_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesInvoices_InventoryLocations_InventoryLocationId",
                        column: x => x.InventoryLocationId,
                        principalTable: "InventoryLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesInvoiceLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SalesInvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineNumber = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductNameSnapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ProductSkuSnapshot = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    BaselineUnitPriceSnapshot = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    MinimumSellingPriceSnapshot = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    SellingUnitPrice = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    PriceRevisionSnapshot = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesInvoiceLines", x => x.Id);
                    table.CheckConstraint("CK_SalesLines_Price", "\"BaselineUnitPriceSnapshot\" >= 0 AND \"MinimumSellingPriceSnapshot\" >= 0 AND \"SellingUnitPrice\" >= \"MinimumSellingPriceSnapshot\"");
                    table.CheckConstraint("CK_SalesLines_Quantity", "\"Quantity\" > 0");
                    table.CheckConstraint("CK_SalesLines_Sequence", "\"LineNumber\" > 0 AND \"PriceRevisionSnapshot\" > 0");
                    table.ForeignKey(
                        name: "FK_SalesInvoiceLines_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesInvoiceLines_SalesInvoices_SalesInvoiceId",
                        column: x => x.SalesInvoiceId,
                        principalTable: "SalesInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UX_StockMovements_SalesInvoiceLine",
                table: "StockMovements",
                column: "SalesInvoiceLineId",
                unique: true,
                filter: "\"SalesInvoiceLineId\" IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockMovements_Source",
                table: "StockMovements",
                sql: "(\"MovementType\" = 1 AND \"PurchaseInvoiceLineId\" IS NOT NULL\n    AND \"StockTransferLineId\" IS NULL AND \"SalesInvoiceLineId\" IS NULL)\nOR (\"MovementType\" IN (2, 3, 4) AND \"StockTransferLineId\" IS NOT NULL\n    AND \"PurchaseInvoiceLineId\" IS NULL AND \"SalesInvoiceLineId\" IS NULL)\nOR (\"MovementType\" = 5 AND \"SalesInvoiceLineId\" IS NOT NULL\n    AND \"PurchaseInvoiceLineId\" IS NULL AND \"StockTransferLineId\" IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockMovements_TypeAndQuantity",
                table: "StockMovements",
                sql: "(\"MovementType\" IN (1, 3, 4) AND \"QuantityChange\" > 0) OR (\"MovementType\" IN (2, 5) AND \"QuantityChange\" < 0)");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoiceLines_ProductId",
                table: "SalesInvoiceLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoiceLines_SalesInvoiceId_LineNumber",
                table: "SalesInvoiceLines",
                columns: new[] { "SalesInvoiceId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoiceLines_SalesInvoiceId_ProductId",
                table: "SalesInvoiceLines",
                columns: new[] { "SalesInvoiceId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_BranchId_ClientSaleId",
                table: "SalesInvoices",
                columns: new[] { "BranchId", "ClientSaleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_BranchId_SoldAtUtc_Id",
                table: "SalesInvoices",
                columns: new[] { "BranchId", "SoldAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_InventoryLocationId",
                table: "SalesInvoices",
                column: "InventoryLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_Number",
                table: "SalesInvoices",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_SoldByUserId",
                table: "SalesInvoices",
                column: "SoldByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_SalesInvoiceLines_SalesInvoiceLineId",
                table: "StockMovements",
                column: "SalesInvoiceLineId",
                principalTable: "SalesInvoiceLines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_SalesInvoiceLines_SalesInvoiceLineId",
                table: "StockMovements");

            migrationBuilder.DropTable(
                name: "SalesInvoiceLines");

            migrationBuilder.DropTable(
                name: "SalesInvoices");

            migrationBuilder.DropIndex(
                name: "UX_StockMovements_SalesInvoiceLine",
                table: "StockMovements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StockMovements_Source",
                table: "StockMovements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StockMovements_TypeAndQuantity",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "SalesInvoiceLineId",
                table: "StockMovements");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockMovements_Source",
                table: "StockMovements",
                sql: "(\"MovementType\" = 1 AND \"PurchaseInvoiceLineId\" IS NOT NULL AND \"StockTransferLineId\" IS NULL) OR (\"MovementType\" IN (2, 3, 4) AND \"PurchaseInvoiceLineId\" IS NULL AND \"StockTransferLineId\" IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockMovements_TypeAndQuantity",
                table: "StockMovements",
                sql: "(\"MovementType\" IN (1, 3, 4) AND \"QuantityChange\" > 0) OR (\"MovementType\" = 2 AND \"QuantityChange\" < 0)");
        }
    }
}
