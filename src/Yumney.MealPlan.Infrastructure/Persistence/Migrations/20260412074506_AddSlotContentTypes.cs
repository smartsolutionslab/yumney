using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSlotContentTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "MealSlots",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FreetextLabel",
                table: "MealSlots",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LeftoverSourceDay",
                table: "MealSlots",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LeftoverSourceMealType",
                table: "MealSlots",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "MealSlots");

            migrationBuilder.DropColumn(
                name: "FreetextLabel",
                table: "MealSlots");

            migrationBuilder.DropColumn(
                name: "LeftoverSourceDay",
                table: "MealSlots");

            migrationBuilder.DropColumn(
                name: "LeftoverSourceMealType",
                table: "MealSlots");
        }
    }
}
