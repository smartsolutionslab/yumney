using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSolutionsLab.Yumney.Shared.Persistence;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.Configurations;

internal sealed class ShoppingListConfiguration : IEntityTypeConfiguration<ShoppingList>
{
    public void Configure(EntityTypeBuilder<ShoppingList> entity)
    {
        entity.ToTable("ShoppingLists");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id)
            .HasConversion(v => v.Value, v => ShoppingListIdentifier.From(v));

        entity.Property(e => e.Title)
            .ConfigureRequiredStringValueObject(
                v => v.Value, ShoppingListTitle.From, ShoppingListTitle.MaxLength);

        entity.Property(e => e.Owner)
            .ConfigureRequiredStringValueObject(
                v => v.Value, OwnerIdentifier.From, OwnerIdentifier.MaxLength);

        entity.Property(e => e.RecipeReference)
            .HasConversion(
                v => v != null ? v.Value : (Guid?)null,
                v => RecipeReference.FromNullable(v))
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
                .HasConversion(v => v.Value, v => ShoppingListItemIdentifier.From(v));

            item.Property(i => i.Name)
                .ConfigureRequiredStringValueObject(
                    v => v.Value, ItemName.From, ItemName.MaxLength);

            item.Property(i => i.Amount)
                .ConfigureNullableDecimalValueObject(
                    v => v.Value, Amount.FromNullable);

            item.Property(i => i.Unit)
                .ConfigureNullableStringValueObject(
                    v => v.Value, Unit.FromNullable, Unit.MaxLength);
        });

        entity.Property<uint>("xmin")
            .HasColumnType("xid")
            .IsRowVersion();
        entity.Ignore(e => e.DomainEvents);
    }
}
