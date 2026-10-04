using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTransferReturns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_StockTransfers_Status",
                table: "StockTransfers");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StockTransfers_StatusTimestamps",
                table: "StockTransfers");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StockMovements_Source",
                table: "StockMovements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StockMovements_TypeAndQuantity",
                table: "StockMovements");

            migrationBuilder.AddColumn<string>(
                name: "ReturnReason",
                table: "StockTransfers",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReturnRequestedAtUtc",
                table: "StockTransfers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReturnedAtUtc",
                table: "StockTransfers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockTransfers_ReturnDetails",
                table: "StockTransfers",
                sql: "(\"Status\" IN (1, 2, 3, 4) AND \"ReturnRequestedAtUtc\" IS NULL AND \"ReturnedAtUtc\" IS NULL AND \"ReturnReason\" IS NULL) OR (\"Status\" = 5 AND \"ReturnRequestedAtUtc\" IS NOT NULL AND \"ReturnRequestedAtUtc\" >= \"ShippedAtUtc\" AND \"ReturnedAtUtc\" IS NULL AND \"ReturnReason\" IS NOT NULL AND length(btrim(\"ReturnReason\")) > 0) OR (\"Status\" = 6 AND \"ReturnRequestedAtUtc\" IS NOT NULL AND \"ReturnRequestedAtUtc\" >= \"ShippedAtUtc\" AND \"ReturnedAtUtc\" IS NOT NULL AND \"ReturnedAtUtc\" >= \"ReturnRequestedAtUtc\" AND \"ReturnReason\" IS NOT NULL AND length(btrim(\"ReturnReason\")) > 0)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockTransfers_Status",
                table: "StockTransfers",
                sql: "\"Status\" IN (1, 2, 3, 4, 5, 6)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockTransfers_StatusTimestamps",
                table: "StockTransfers",
                sql: "(\"Status\" IN (1, 4) AND \"ShippedAtUtc\" IS NULL AND \"ReceivedAtUtc\" IS NULL) OR (\"Status\" IN (2, 5, 6) AND \"ShippedAtUtc\" IS NOT NULL AND \"ReceivedAtUtc\" IS NULL) OR (\"Status\" = 3 AND \"ShippedAtUtc\" IS NOT NULL AND \"ReceivedAtUtc\" IS NOT NULL AND \"ReceivedAtUtc\" >= \"ShippedAtUtc\")");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockMovements_Source",
                table: "StockMovements",
                sql: "(\"MovementType\" = 1 AND \"PurchaseInvoiceLineId\" IS NOT NULL AND \"StockTransferLineId\" IS NULL) OR (\"MovementType\" IN (2, 3, 4) AND \"PurchaseInvoiceLineId\" IS NULL AND \"StockTransferLineId\" IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockMovements_TypeAndQuantity",
                table: "StockMovements",
                sql: "(\"MovementType\" IN (1, 3, 4) AND \"QuantityChange\" > 0) OR (\"MovementType\" = 2 AND \"QuantityChange\" < 0)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_StockTransfers_ReturnDetails",
                table: "StockTransfers");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StockTransfers_Status",
                table: "StockTransfers");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StockTransfers_StatusTimestamps",
                table: "StockTransfers");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StockMovements_Source",
                table: "StockMovements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StockMovements_TypeAndQuantity",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "ReturnReason",
                table: "StockTransfers");

            migrationBuilder.DropColumn(
                name: "ReturnRequestedAtUtc",
                table: "StockTransfers");

            migrationBuilder.DropColumn(
                name: "ReturnedAtUtc",
                table: "StockTransfers");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockTransfers_Status",
                table: "StockTransfers",
                sql: "\"Status\" IN (1, 2, 3, 4)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockTransfers_StatusTimestamps",
                table: "StockTransfers",
                sql: "(\"Status\" IN (1, 4) AND \"ShippedAtUtc\" IS NULL AND \"ReceivedAtUtc\" IS NULL) OR (\"Status\" = 2 AND \"ShippedAtUtc\" IS NOT NULL AND \"ReceivedAtUtc\" IS NULL) OR (\"Status\" = 3 AND \"ShippedAtUtc\" IS NOT NULL AND \"ReceivedAtUtc\" IS NOT NULL AND \"ReceivedAtUtc\" >= \"ShippedAtUtc\")");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockMovements_Source",
                table: "StockMovements",
                sql: "(\"MovementType\" = 1 AND \"PurchaseInvoiceLineId\" IS NOT NULL AND \"StockTransferLineId\" IS NULL) OR (\"MovementType\" IN (2, 3) AND \"PurchaseInvoiceLineId\" IS NULL AND \"StockTransferLineId\" IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockMovements_TypeAndQuantity",
                table: "StockMovements",
                sql: "(\"MovementType\" IN (1, 3) AND \"QuantityChange\" > 0) OR (\"MovementType\" = 2 AND \"QuantityChange\" < 0)");
        }
    }
}
