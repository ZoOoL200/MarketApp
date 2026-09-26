using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExtendStockMovementsForTransfers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_StockMovements_PurchaseInvoiceLine",
                table: "StockMovements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StockMovements_TypeAndQuantity",
                table: "StockMovements");

            migrationBuilder.AlterColumn<Guid>(
                name: "PurchaseInvoiceLineId",
                table: "StockMovements",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "StockTransferLineId",
                table: "StockMovements",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "UX_StockMovements_PurchaseInvoiceLine",
                table: "StockMovements",
                column: "PurchaseInvoiceLineId",
                unique: true,
                filter: "\"PurchaseInvoiceLineId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_StockMovements_TransferLine_Type",
                table: "StockMovements",
                columns: new[] { "StockTransferLineId", "MovementType" },
                unique: true,
                filter: "\"StockTransferLineId\" IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockMovements_Source",
                table: "StockMovements",
                sql: "(\"MovementType\" = 1 AND \"PurchaseInvoiceLineId\" IS NOT NULL AND \"StockTransferLineId\" IS NULL) OR (\"MovementType\" IN (2, 3) AND \"PurchaseInvoiceLineId\" IS NULL AND \"StockTransferLineId\" IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockMovements_TypeAndQuantity",
                table: "StockMovements",
                sql: "(\"MovementType\" IN (1, 3) AND \"QuantityChange\" > 0) OR (\"MovementType\" = 2 AND \"QuantityChange\" < 0)");

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_StockTransferLines_StockTransferLineId",
                table: "StockMovements",
                column: "StockTransferLineId",
                principalTable: "StockTransferLines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_StockTransferLines_StockTransferLineId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "UX_StockMovements_PurchaseInvoiceLine",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "UX_StockMovements_TransferLine_Type",
                table: "StockMovements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StockMovements_Source",
                table: "StockMovements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StockMovements_TypeAndQuantity",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "StockTransferLineId",
                table: "StockMovements");

            migrationBuilder.AlterColumn<Guid>(
                name: "PurchaseInvoiceLineId",
                table: "StockMovements",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "UX_StockMovements_PurchaseInvoiceLine",
                table: "StockMovements",
                column: "PurchaseInvoiceLineId",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockMovements_TypeAndQuantity",
                table: "StockMovements",
                sql: "\"MovementType\" = 1 AND \"QuantityChange\" > 0");
        }
    }
}
