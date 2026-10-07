using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CompleteProductPhotoStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                table: "ProductPhotos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsReady",
                table: "ProductPhotos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_ProductPhotos_IsReady_DeletedAtUtc_CreatedAtUtc",
                table: "ProductPhotos",
                columns: new[] { "IsReady", "DeletedAtUtc", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductPhotos_IsReady_DeletedAtUtc_CreatedAtUtc",
                table: "ProductPhotos");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "ProductPhotos");

            migrationBuilder.DropColumn(
                name: "IsReady",
                table: "ProductPhotos");
        }
    }
}
