using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIngredientBalanceReadModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IngredientBalanceReadItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ItemName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NameKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Category = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    BoughtTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    ConsumedTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    RemovedTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngredientBalanceReadItems", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IngredientBalanceReadItems_OwnerId",
                table: "IngredientBalanceReadItems",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_IngredientBalanceReadItems_OwnerId_NameKey_Unit",
                table: "IngredientBalanceReadItems",
                columns: new[] { "OwnerId", "NameKey", "Unit" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IngredientBalanceReadItems");
        }
    }
}
