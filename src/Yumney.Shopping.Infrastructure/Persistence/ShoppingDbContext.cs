using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Shared.Persistence;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence;

public sealed class ShoppingDbContext(DbContextOptions<ShoppingDbContext> options) : DbContext(options)
{
    public DbSet<ShoppingList> ShoppingLists => Set<ShoppingList>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ShoppingList>(entity =>
        {
            entity.ToTable("ShoppingLists");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                .HasConversion(v => v.Value, v => new ShoppingListIdentifier(v));

            entity.Property(e => e.Title)
                .ConfigureRequiredStringValueObject(
                    v => v.Value, v => new ShoppingListTitle(v), ShoppingListTitle.MaxLength);

            entity.Property(e => e.Owner)
                .ConfigureRequiredStringValueObject(
                    v => v.Value, v => OwnerIdentifier.From(v), OwnerIdentifier.MaxLength);

            entity.Property(e => e.RecipeReference)
                .HasConversion(
                    v => v != null ? v.Value : (Guid?)null,
                    v => v.HasValue ? new RecipeReference(v.Value) : null)
                .HasColumnName("RecipeIdentifier");

            entity.HasIndex(e => e.Owner);
            entity.Property(e => e.CreatedAt).IsRequired();

            entity.OwnsMany(e => e.Items, item =>
            {
                item.ToTable("ShoppingListItems");
                item.WithOwner().HasForeignKey("ShoppingListId");
                item.HasKey(nameof(ShoppingListItem.Id));

                item.Property(i => i.Name)
                    .ConfigureRequiredStringValueObject(
                        v => v.Value, v => new ItemName(v), ItemName.MaxLength);

                item.Property(i => i.Amount)
                    .ConfigureNullableDecimalValueObject(
                        v => v.Value, Amount.FromNullable);

                item.Property(i => i.Unit)
                    .ConfigureNullableStringValueObject(
                        v => v.Value, Unit.FromNullable, Unit.MaxLength);
            });

            entity.Ignore(e => e.DomainEvents);
        });
    }
}
