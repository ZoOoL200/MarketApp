using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductPhotoMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductPhotos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductPhotos", x => x.Id);
                    table.CheckConstraint("CK_ProductPhotos_ContentHash", "\"ContentHash\" ~ '^[0-9A-F]{64}$'");
                    table.CheckConstraint("CK_ProductPhotos_ContentType", "\"ContentType\" IN ('image/jpeg', 'image/png', 'image/webp')");
                    table.CheckConstraint("CK_ProductPhotos_FileSizeBytes", "\"FileSizeBytes\" > 0");
                    table.CheckConstraint("CK_ProductPhotos_SortOrder", "\"SortOrder\" >= 0");
                    table.CheckConstraint("CK_ProductPhotos_StorageKey", "length(btrim(\"StorageKey\")) > 0");
                    table.ForeignKey(
                        name: "FK_ProductPhotos_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductPhotos_ProductId_SortOrder_Id",
                table: "ProductPhotos",
                columns: new[] { "ProductId", "SortOrder", "Id" });

            migrationBuilder.CreateIndex(
                name: "UX_ProductPhotos_StorageKey",
                table: "ProductPhotos",
                column: "StorageKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductPhotos");
        }
    }
}
