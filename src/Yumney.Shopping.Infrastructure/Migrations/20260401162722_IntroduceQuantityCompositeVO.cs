using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IntroduceQuantityCompositeVO : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "ShoppingLists",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingLists_Owner_CreatedAt",
                table: "ShoppingLists",
                columns: new[] { "Owner", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingLists_Owner_Title",
                table: "ShoppingLists",
                columns: new[] { "Owner", "Title" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShoppingLists_Owner_CreatedAt",
                table: "ShoppingLists");

            migrationBuilder.DropIndex(
                name: "IX_ShoppingLists_Owner_Title",
                table: "ShoppingLists");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "ShoppingLists");
        }
    }
}
