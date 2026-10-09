using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStakeholderExpenseRegister : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StakeholderExpenses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientExpenseId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    Category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    PaidTo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ReferenceNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    VoidedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    VoidedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VoidReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StakeholderExpenses", x => x.Id);
                    table.CheckConstraint("CK_StakeholderExpenses_Amount", "\"Amount\" > 0");
                    table.CheckConstraint("CK_StakeholderExpenses_Category", "\"Category\" IN ('Rent', 'Other')");
                    table.CheckConstraint("CK_StakeholderExpenses_Void", "(\"VoidedAtUtc\" IS NULL AND \"VoidedByUserId\" IS NULL AND \"VoidReason\" IS NULL) OR\n(\"VoidedAtUtc\" IS NOT NULL AND \"VoidedByUserId\" IS NOT NULL AND \"VoidReason\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_StakeholderExpenses_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StakeholderExpenses_AspNetUsers_VoidedByUserId",
                        column: x => x.VoidedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StakeholderExpenses_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StakeholderExpenses_BranchId_OccurredAtUtc_Id",
                table: "StakeholderExpenses",
                columns: new[] { "BranchId", "OccurredAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_StakeholderExpenses_ClientExpenseId",
                table: "StakeholderExpenses",
                column: "ClientExpenseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StakeholderExpenses_CreatedByUserId",
                table: "StakeholderExpenses",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StakeholderExpenses_OccurredAtUtc_Id",
                table: "StakeholderExpenses",
                columns: new[] { "OccurredAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_StakeholderExpenses_VoidedByUserId",
                table: "StakeholderExpenses",
                column: "VoidedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StakeholderExpenses");
        }
    }
}
