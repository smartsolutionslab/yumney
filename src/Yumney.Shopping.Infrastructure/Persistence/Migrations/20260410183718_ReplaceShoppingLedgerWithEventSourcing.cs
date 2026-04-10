using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceShoppingLedgerWithEventSourcing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LedgerTransactions");

            migrationBuilder.DropTable(
                name: "ShoppingLedgers");

            migrationBuilder.CreateTable(
                name: "ShoppingAggregates",
                columns: table => new
                {
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShoppingAggregates", x => x.AggregateId);
                });

            migrationBuilder.CreateTable(
                name: "ShoppingEvents",
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
                    table.PrimaryKey("PK_ShoppingEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShoppingListReadItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ItemName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TotalQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    Unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Category = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    IsBought = table.Column<bool>(type: "boolean", nullable: false),
                    SourcesJson = table.Column<string>(type: "jsonb", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShoppingListReadItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShoppingSnapshots",
                columns: table => new
                {
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    State = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShoppingSnapshots", x => x.AggregateId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingAggregates_OwnerId",
                table: "ShoppingAggregates",
                column: "OwnerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingEvents_AggregateId_Version",
                table: "ShoppingEvents",
                columns: new[] { "AggregateId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingEvents_OccurredAt",
                table: "ShoppingEvents",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingListReadItems_OwnerId",
                table: "ShoppingListReadItems",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingListReadItems_OwnerId_ItemName_Unit",
                table: "ShoppingListReadItems",
                columns: new[] { "OwnerId", "ItemName", "Unit" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShoppingAggregates");

            migrationBuilder.DropTable(
                name: "ShoppingEvents");

            migrationBuilder.DropTable(
                name: "ShoppingListReadItems");

            migrationBuilder.DropTable(
                name: "ShoppingSnapshots");

            migrationBuilder.CreateTable(
                name: "ShoppingLedgers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Owner = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShoppingLedgers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LedgerTransactions",
                columns: table => new
                {
                    ShoppingLedgerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ItemName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    Source = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LedgerTransactions", x => new { x.ShoppingLedgerId, x.Id });
                    table.ForeignKey(
                        name: "FK_LedgerTransactions_ShoppingLedgers_ShoppingLedgerId",
                        column: x => x.ShoppingLedgerId,
                        principalTable: "ShoppingLedgers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LedgerTransactions_OccurredAt",
                table: "LedgerTransactions",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingLedgers_Owner",
                table: "ShoppingLedgers",
                column: "Owner",
                unique: true);
        }
    }
}
