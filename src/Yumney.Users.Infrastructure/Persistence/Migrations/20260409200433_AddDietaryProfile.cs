using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartSolutionsLab.Yumney.Users.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDietaryProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CookingEffort",
                table: "AppUserProfiles",
                type: "character varying(25)",
                maxLength: 25,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DietaryRestrictions",
                table: "AppUserProfiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DietaryType",
                table: "AppUserProfiles",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxRedMeatMeals",
                table: "AppUserProfiles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinVeggieMeals",
                table: "AppUserProfiles",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CookingEffort",
                table: "AppUserProfiles");

            migrationBuilder.DropColumn(
                name: "DietaryRestrictions",
                table: "AppUserProfiles");

            migrationBuilder.DropColumn(
                name: "DietaryType",
                table: "AppUserProfiles");

            migrationBuilder.DropColumn(
                name: "MaxRedMeatMeals",
                table: "AppUserProfiles");

            migrationBuilder.DropColumn(
                name: "MinVeggieMeals",
                table: "AppUserProfiles");
        }
    }
}
