using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddShoppingLedgerNaturalKeyUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: drop any duplicates on (OwnerId, lower(ItemName), Unit) so
            // the unique index can be created. The projection's previous
            // read-then-modify pattern could in theory produce duplicates under
            // concurrent ShoppingItemAdded delivery; this keeps the most-recently-
            // updated row per natural key and discards the rest. (Postgres has no
            // MIN(uuid), so we lean on ROW_NUMBER + DELETE rather than aggregating
            // on Id directly.) For a fresh dev DB the SELECT returns zero rows
            // and DELETE is a no-op.
            migrationBuilder.Sql(
                """
                DELETE FROM "ShoppingListReadItems" target
                USING (
                    SELECT
                        "Id",
                        ROW_NUMBER() OVER (
                            PARTITION BY "OwnerId", lower("ItemName"), COALESCE("Unit", '')
                            ORDER BY "LastUpdated" DESC, "Id"
                        ) AS rn
                    FROM "ShoppingListReadItems"
                ) ranked
                WHERE target."Id" = ranked."Id" AND ranked.rn > 1;
                """);

            // Step 2: enforce the natural key. Expression form because Unit is
            // nullable and we need NULL units to collide with each other (NULLS
            // NOT DISTINCT works in PG 15+ but COALESCE is portable to PG 14
            // and is what the projection's ON CONFLICT target will mirror).
            migrationBuilder.Sql(
                """
                CREATE UNIQUE INDEX "IX_ShoppingListReadItems_OwnerId_LowerItemName_Unit"
                ON "ShoppingListReadItems" ("OwnerId", lower("ItemName"), COALESCE("Unit", ''));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP INDEX "IX_ShoppingListReadItems_OwnerId_LowerItemName_Unit";
                """);
        }
    }
}
