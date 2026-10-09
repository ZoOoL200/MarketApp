using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddManagerProfitShares : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ManagerSharePercentSnapshot",
                table: "SalesInvoices",
                type: "numeric(7,4)",
                precision: 7,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "ManagerUserIdSnapshot",
                table: "SalesInvoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ManagerProfitShares",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientChangeId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    ManagerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Percent = table.Column<decimal>(type: "numeric(7,4)", precision: 7, scale: 4, nullable: false),
                    EffectiveFromUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManagerProfitShares", x => x.Id);
                    table.CheckConstraint("CK_ManagerShare_Percent", "\"Percent\" BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_ManagerProfitShares_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ManagerProfitShares_AspNetUsers_ManagerUserId",
                        column: x => x.ManagerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ManagerProfitShares_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_ManagerUserIdSnapshot",
                table: "SalesInvoices",
                column: "ManagerUserIdSnapshot");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SalesInvoices_ManagerShare",
                table: "SalesInvoices",
                sql: "\"ManagerSharePercentSnapshot\" BETWEEN 0 AND 100 AND\n(\"ManagerUserIdSnapshot\" IS NOT NULL OR \"ManagerSharePercentSnapshot\" = 0)");

            migrationBuilder.CreateIndex(
                name: "IX_ManagerProfitShares_BranchId_ClientChangeId",
                table: "ManagerProfitShares",
                columns: new[] { "BranchId", "ClientChangeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ManagerProfitShares_BranchId_EffectiveFromUtc",
                table: "ManagerProfitShares",
                columns: new[] { "BranchId", "EffectiveFromUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ManagerProfitShares_CreatedByUserId",
                table: "ManagerProfitShares",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ManagerProfitShares_ManagerUserId",
                table: "ManagerProfitShares",
                column: "ManagerUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesInvoices_AspNetUsers_ManagerUserIdSnapshot",
                table: "SalesInvoices",
                column: "ManagerUserIdSnapshot",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesInvoices_AspNetUsers_ManagerUserIdSnapshot",
                table: "SalesInvoices");

            migrationBuilder.DropTable(
                name: "ManagerProfitShares");

            migrationBuilder.DropIndex(
                name: "IX_SalesInvoices_ManagerUserIdSnapshot",
                table: "SalesInvoices");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SalesInvoices_ManagerShare",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "ManagerSharePercentSnapshot",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "ManagerUserIdSnapshot",
                table: "SalesInvoices");
        }
    }
}
