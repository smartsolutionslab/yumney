using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceWeeklyPlanWithEventSourcing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MealSlots");

            migrationBuilder.DropTable(
                name: "WeeklyPlans");

            migrationBuilder.CreateTable(
                name: "MealPlanAggregates",
                columns: table => new
                {
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Week = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MealPlanAggregates", x => x.AggregateId);
                });

            migrationBuilder.CreateTable(
                name: "MealPlanEvents",
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
                    table.PrimaryKey("PK_MealPlanEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MealPlanSlotReadItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Week = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Day = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    MealType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    RecipeIdentifier = table.Column<Guid>(type: "uuid", nullable: true),
                    RecipeTitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Servings = table.Column<int>(type: "integer", nullable: false),
                    FreetextLabel = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    LeftoverLabel = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    LeftoverSourceDay = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    LeftoverSourceMealType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    State = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MealPlanSlotReadItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MealPlanWeekReadItems",
                columns: table => new
                {
                    OwnerId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Week = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    IsExtendedMode = table.Column<bool>(type: "boolean", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MealPlanWeekReadItems", x => new { x.OwnerId, x.Week });
                });

            migrationBuilder.CreateIndex(
                name: "IX_MealPlanAggregates_OwnerId_Week",
                table: "MealPlanAggregates",
                columns: new[] { "OwnerId", "Week" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MealPlanEvents_AggregateId_Version",
                table: "MealPlanEvents",
                columns: new[] { "AggregateId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MealPlanEvents_OccurredAt",
                table: "MealPlanEvents",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_MealPlanSlotReadItems_OwnerId_Week",
                table: "MealPlanSlotReadItems",
                columns: new[] { "OwnerId", "Week" });

            migrationBuilder.CreateIndex(
                name: "IX_MealPlanSlotReadItems_OwnerId_Week_Day_MealType",
                table: "MealPlanSlotReadItems",
                columns: new[] { "OwnerId", "Week", "Day", "MealType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MealPlanAggregates");

            migrationBuilder.DropTable(
                name: "MealPlanEvents");

            migrationBuilder.DropTable(
                name: "MealPlanSlotReadItems");

            migrationBuilder.DropTable(
                name: "MealPlanWeekReadItems");

            migrationBuilder.CreateTable(
                name: "WeeklyPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IsExtendedMode = table.Column<bool>(type: "boolean", nullable: false),
                    Owner = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Week = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeeklyPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MealSlots",
                columns: table => new
                {
                    WeeklyPlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Day = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    FreetextLabel = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    LeftoverLabel = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    LeftoverSourceDay = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    LeftoverSourceMealType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    MealType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Servings = table.Column<int>(type: "integer", nullable: false),
                    State = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    RecipeIdentifier = table.Column<Guid>(type: "uuid", nullable: true),
                    RecipeTitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MealSlots", x => new { x.WeeklyPlanId, x.Id });
                    table.ForeignKey(
                        name: "FK_MealSlots_WeeklyPlans_WeeklyPlanId",
                        column: x => x.WeeklyPlanId,
                        principalTable: "WeeklyPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyPlans_Owner_Week",
                table: "WeeklyPlans",
                columns: new[] { "Owner", "Week" },
                unique: true);
        }
    }
}
