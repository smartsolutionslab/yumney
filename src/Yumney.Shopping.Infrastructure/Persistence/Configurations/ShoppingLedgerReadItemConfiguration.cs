using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.Configurations;

internal sealed class ShoppingLedgerReadItemConfiguration : IEntityTypeConfiguration<ShoppingLedgerReadItem>
{
	public void Configure(EntityTypeBuilder<ShoppingLedgerReadItem> builder)
	{
		// Physical table name kept as-is to avoid a rename migration.
		// The C# type was renamed in the cleanup pass; the schema is stable.
		builder.ToTable("ShoppingListReadItems");
		builder.HasKey(item => item.Id);
		builder.Property(item => item.OwnerId).HasMaxLength(255).IsRequired();
		builder.Property(item => item.ItemName).HasMaxLength(200).IsRequired();
		builder.Property(item => item.Unit).HasMaxLength(20);
		builder.Property(item => item.Category).HasMaxLength(30).IsRequired();
		builder.Property(item => item.SourcesJson).HasColumnType("jsonb");
		builder.Property(item => item.LastUpdated).IsRequired();

		builder.HasIndex(item => item.OwnerId);
		builder.HasIndex(item => new { item.OwnerId, item.ItemName, item.Unit });

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
