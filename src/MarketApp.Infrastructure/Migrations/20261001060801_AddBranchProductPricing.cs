using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchProductPricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BranchProductPrices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    BaselineUnitPrice = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    MinimumSellingPrice = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BranchProductPrices", x => x.Id);
                    table.CheckConstraint("CK_BranchProductPrices_Baseline", "\"BaselineUnitPrice\" IS NULL OR \"BaselineUnitPrice\" >= 0");
                    table.CheckConstraint("CK_BranchProductPrices_HasPrice", "\"BaselineUnitPrice\" IS NOT NULL OR \"MinimumSellingPrice\" IS NOT NULL");
                    table.CheckConstraint("CK_BranchProductPrices_Minimum", "\"MinimumSellingPrice\" IS NULL OR \"MinimumSellingPrice\" >= 0");
                    table.CheckConstraint("CK_BranchProductPrices_Revision", "\"Revision\" > 0");
                    table.ForeignKey(
                        name: "FK_BranchProductPrices_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BranchProductPrices_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BranchProductPriceHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchProductPriceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    OldBaselineUnitPrice = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    NewBaselineUnitPrice = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    OldMinimumSellingPrice = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    NewMinimumSellingPrice = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    ChangeType = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ChangedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    StockTransferLineId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BranchProductPriceHistory", x => x.Id);
                    table.CheckConstraint("CK_BranchPriceHistory_HasNewPrice", "\"NewBaselineUnitPrice\" IS NOT NULL OR \"NewMinimumSellingPrice\" IS NOT NULL");
                    table.CheckConstraint("CK_BranchPriceHistory_Prices", "(\"OldBaselineUnitPrice\" IS NULL OR \"OldBaselineUnitPrice\" >= 0) AND (\"NewBaselineUnitPrice\" IS NULL OR \"NewBaselineUnitPrice\" >= 0) AND (\"OldMinimumSellingPrice\" IS NULL OR \"OldMinimumSellingPrice\" >= 0) AND (\"NewMinimumSellingPrice\" IS NULL OR \"NewMinimumSellingPrice\" >= 0)");
                    table.CheckConstraint("CK_BranchPriceHistory_Reason", "length(btrim(\"Reason\")) > 0");
                    table.CheckConstraint("CK_BranchPriceHistory_Revision", "\"Revision\" > 0");
                    table.CheckConstraint("CK_BranchPriceHistory_Source", "(\"ChangeType\" IN (1, 2) AND \"StockTransferLineId\" IS NULL) OR (\"ChangeType\" = 3 AND \"StockTransferLineId\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_BranchProductPriceHistory_BranchProductPrices_BranchProduct~",
                        column: x => x.BranchProductPriceId,
                        principalTable: "BranchProductPrices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BranchProductPriceHistory_StockTransferLines_StockTransferL~",
                        column: x => x.StockTransferLineId,
                        principalTable: "StockTransferLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BranchProductPriceHistory_StockTransferLineId",
                table: "BranchProductPriceHistory",
                column: "StockTransferLineId");

            migrationBuilder.CreateIndex(
                name: "UX_BranchPriceHistory_Price_Revision",
                table: "BranchProductPriceHistory",
                columns: new[] { "BranchProductPriceId", "Revision" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BranchProductPrices_ProductId",
                table: "BranchProductPrices",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "UX_BranchProductPrices_Branch_Product",
                table: "BranchProductPrices",
                columns: new[] { "BranchId", "ProductId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BranchProductPriceHistory");

            migrationBuilder.DropTable(
                name: "BranchProductPrices");
        }
    }
}
