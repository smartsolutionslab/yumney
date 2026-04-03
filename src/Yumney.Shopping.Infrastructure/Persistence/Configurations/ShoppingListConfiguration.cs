using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
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
            .HasConversion(v => v.Value, v => ShoppingListTitle.From(v))
            .HasMaxLength(ShoppingListTitle.MaxLength)
            .IsRequired();

        entity.Property(e => e.Owner)
            .HasConversion(v => v.Value, v => OwnerIdentifier.From(v))
            .HasMaxLength(OwnerIdentifier.MaxLength)
            .IsRequired();

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
                .HasConversion(v => v.Value, v => ItemName.From(v))
                .HasMaxLength(ItemName.MaxLength)
                .IsRequired();

            item.OwnsOne(i => i.Quantity, q =>
            {
                q.Property(x => x.Amount)
                    .HasConversion(v => v.Value, v => Amount.From(v))
                    .HasColumnName("Amount");

                q.Property(x => x.Unit)
                    .HasConversion(v => v != null ? v.Value : null, v => Unit.FromNullable(v))
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
