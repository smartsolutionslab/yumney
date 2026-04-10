using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SmartSolutionsLab.Yumney.Users.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStaplesList : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StaplesLists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Owner = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaplesLists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StapleItems",
                columns: table => new
                {
                    StaplesListId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StapleItems", x => new { x.StaplesListId, x.Id });
                    table.ForeignKey(
                        name: "FK_StapleItems_StaplesLists_StaplesListId",
                        column: x => x.StaplesListId,
                        principalTable: "StaplesLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StaplesLists_Owner",
                table: "StaplesLists",
                column: "Owner",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StapleItems");

            migrationBuilder.DropTable(
                name: "StaplesLists");
        }
    }
}
