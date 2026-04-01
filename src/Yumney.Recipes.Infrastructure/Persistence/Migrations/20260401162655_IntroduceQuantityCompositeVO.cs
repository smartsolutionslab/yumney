using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IntroduceQuantityCompositeVO : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Recipes",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_Owner_CreatedAt",
                table: "Recipes",
                columns: new[] { "Owner", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_Owner_Title",
                table: "Recipes",
                columns: new[] { "Owner", "Title" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Recipes_Owner_CreatedAt",
                table: "Recipes");

            migrationBuilder.DropIndex(
                name: "IX_Recipes_Owner_Title",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Recipes");
        }
    }
}
