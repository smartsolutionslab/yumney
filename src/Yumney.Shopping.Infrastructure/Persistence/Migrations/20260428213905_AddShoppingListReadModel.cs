using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddShoppingListReadModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShoppingListItemReadItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ListId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    QuantityAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    QuantityUnit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsChecked = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShoppingListItemReadItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShoppingListSummaryReadItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RecipeIdentifier = table.Column<Guid>(type: "uuid", nullable: true),
                    ItemCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShoppingListSummaryReadItems", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingListItemReadItems_ListId",
                table: "ShoppingListItemReadItems",
                column: "ListId");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingListItemReadItems_OwnerId_ListId",
                table: "ShoppingListItemReadItems",
                columns: new[] { "OwnerId", "ListId" });

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingListSummaryReadItems_OwnerId",
                table: "ShoppingListSummaryReadItems",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingListSummaryReadItems_OwnerId_CreatedAt",
                table: "ShoppingListSummaryReadItems",
                columns: new[] { "OwnerId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingListSummaryReadItems_OwnerId_Title",
                table: "ShoppingListSummaryReadItems",
                columns: new[] { "OwnerId", "Title" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShoppingListItemReadItems");

            migrationBuilder.DropTable(
                name: "ShoppingListSummaryReadItems");
        }
    }
}
