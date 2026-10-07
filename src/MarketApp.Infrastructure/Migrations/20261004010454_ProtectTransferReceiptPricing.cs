using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ProtectTransferReceiptPricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BaselineRevisionAtCreation",
                table: "StockTransferLines",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BaselineRevision",
                table: "BranchProductPrices",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Recover the most recent actual baseline change. If older data has
            // no history, use its current revision as a conservative starting point.
            // Leave old transfer snapshots NULL: their creation state is unknown.
            migrationBuilder.Sql("""
                UPDATE "BranchProductPrices" AS p
                SET "BaselineRevision" = COALESCE(
                    (SELECT MAX(h."Revision")
                     FROM "BranchProductPriceHistory" AS h
                     WHERE h."BranchProductPriceId" = p."Id"
                       AND h."OldBaselineUnitPrice" IS DISTINCT FROM h."NewBaselineUnitPrice"),
                    p."Revision")
                WHERE p."BaselineUnitPrice" IS NOT NULL;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockTransferLines_BaselineRevisionAtCreation",
                table: "StockTransferLines",
                sql: "\"BaselineRevisionAtCreation\" IS NULL OR \"BaselineRevisionAtCreation\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BranchProductPrices_BaselineRevision",
                table: "BranchProductPrices",
                sql: "\"BaselineRevision\" >= 0 AND \"BaselineRevision\" <= \"Revision\" AND ((\"BaselineUnitPrice\" IS NULL AND \"BaselineRevision\" = 0) OR (\"BaselineUnitPrice\" IS NOT NULL AND \"BaselineRevision\" > 0))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_StockTransferLines_BaselineRevisionAtCreation",
                table: "StockTransferLines");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BranchProductPrices_BaselineRevision",
                table: "BranchProductPrices");

            migrationBuilder.DropColumn(
                name: "BaselineRevisionAtCreation",
                table: "StockTransferLines");

            migrationBuilder.DropColumn(
                name: "BaselineRevision",
                table: "BranchProductPrices");
        }
    }
}
