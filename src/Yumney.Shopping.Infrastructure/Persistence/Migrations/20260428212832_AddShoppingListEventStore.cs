using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddShoppingListEventStore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShoppingListAggregates",
                columns: table => new
                {
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShoppingListAggregates", x => x.AggregateId);
                });

            migrationBuilder.CreateTable(
                name: "ShoppingListEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EventData = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShoppingListEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingListAggregates_OwnerId",
                table: "ShoppingListAggregates",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingListEvents_AggregateId_Version",
                table: "ShoppingListEvents",
                columns: new[] { "AggregateId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingListEvents_OccurredAt",
                table: "ShoppingListEvents",
                column: "OccurredAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShoppingListAggregates");

            migrationBuilder.DropTable(
                name: "ShoppingListEvents");
        }
    }
}
