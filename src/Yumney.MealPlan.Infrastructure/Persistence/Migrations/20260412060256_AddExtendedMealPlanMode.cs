using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExtendedMealPlanMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsExtendedMode",
                table: "WeeklyPlans",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MealType",
                table: "MealSlots",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "Dinner");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsExtendedMode",
                table: "WeeklyPlans");

            migrationBuilder.DropColumn(
                name: "MealType",
                table: "MealSlots");
        }
    }
}
