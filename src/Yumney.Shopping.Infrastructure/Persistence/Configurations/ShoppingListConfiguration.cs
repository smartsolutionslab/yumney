using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.Converters;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.Configurations;

internal sealed class ShoppingListConfiguration : IEntityTypeConfiguration<ShoppingList>
{
	public void Configure(EntityTypeBuilder<ShoppingList> entity)
	{
		entity.ToTable("ShoppingLists");
		entity.HasKey(e => e.Id);
		entity.Property(e => e.Id)
			.HasConversion<ShoppingListIdentifierConverter>();

		entity.Property(e => e.Title)
			.HasConversion<ShoppingListTitleConverter>()
			.HasMaxLength(ShoppingListTitle.MaxLength)
			.IsRequired();

		entity.Property(e => e.Owner)
			.HasConversion<ShoppingOwnerIdentifierConverter>()
			.HasMaxLength(OwnerIdentifier.MaxLength)
			.IsRequired();

		entity.Property(e => e.RecipeReference)
			.HasConversion<RecipeReferenceConverter>()
			.HasColumnName("RecipeIdentifier");

		entity.HasIndex(e => e.Owner);
		entity.HasIndex(e => new { e.Owner, e.CreatedAt });
		entity.HasIndex(e => new { e.Owner, e.Title });
		entity.Property(e => e.CreatedAt).IsRequired();

		entity.OwnsMany(e => e.Items, item =>
		{
			item.ToTable("ShoppingListItems");
			item.WithOwner().HasForeignKey("ShoppingListId");
			item.HasKey(nameof(ShoppingListItem.Id));
			item.Property(i => i.Id)
				.HasConversion<ShoppingListItemIdentifierConverter>();

			item.Property(i => i.Name)
				.HasConversion<ItemNameConverter>()
				.HasMaxLength(ItemName.MaxLength)
				.IsRequired();

			item.OwnsOne(i => i.Quantity, q =>
			{
				q.Property(x => x.Amount)
					.HasConversion<ShoppingAmountConverter>()
					.HasColumnName("Amount");

				q.Property(x => x.Unit)
					.HasConversion<ShoppingUnitConverter>()
					.HasMaxLength(Unit.MaxLength)
					.HasColumnName("Unit");
			});
		});

		entity.Property<uint>("xmin")
			.HasColumnType("xid")
			.IsRowVersion();
		entity.Ignore(e => e.DomainEvents);
	}
}
