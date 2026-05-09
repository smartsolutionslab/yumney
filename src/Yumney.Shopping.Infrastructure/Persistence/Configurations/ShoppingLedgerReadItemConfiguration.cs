using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.Configurations;

internal sealed class ShoppingLedgerReadItemConfiguration : IEntityTypeConfiguration<ShoppingLedgerReadItem>
{
	public void Configure(EntityTypeBuilder<ShoppingLedgerReadItem> entity)
	{
		// Physical table name kept as-is to avoid a rename migration.
		// The C# type was renamed in the cleanup pass; the schema is stable.
		entity.ToTable("ShoppingListReadItems");
		entity.HasKey(e => e.Id);
		entity.Property(e => e.OwnerId).HasMaxLength(255).IsRequired();
		entity.Property(e => e.ItemName).HasMaxLength(200).IsRequired();
		entity.Property(e => e.Unit).HasMaxLength(20);
		entity.Property(e => e.Category).HasMaxLength(30).IsRequired();
		entity.Property(e => e.SourcesJson).HasColumnType("jsonb");
		entity.Property(e => e.LastUpdated).IsRequired();

		entity.HasIndex(e => e.OwnerId);
		entity.HasIndex(e => new { e.OwnerId, e.ItemName, e.Unit });

		// Natural-key uniqueness on (OwnerId, lower(ItemName), COALESCE(Unit, ''))
		// is enforced by the expression-based unique index added in migration
		// 20260509125057_AddShoppingLedgerNaturalKeyUniqueIndex. EF Core can't
		// model an expression-based index without stored generated columns —
		// more schema bloat than the index is worth — so the migration owns
		// it directly. ShoppingLedgerProjectionHandler.HandleAsync(Added)
		// relies on this constraint so two concurrent inserts for the same
		// logical item collide and fall through to the merge path.
	}
}
