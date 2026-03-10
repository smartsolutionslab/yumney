using Microsoft.EntityFrameworkCore;
using Yumney.Recipes.Domain.Recipe;

namespace Yumney.Recipes.Infrastructure.Persistence;

public sealed class RecipesDbContext(DbContextOptions<RecipesDbContext> options) : DbContext(options)
{
    public DbSet<Recipe> Recipes => Set<Recipe>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Recipe>(entity =>
        {
            entity.ToTable("Recipes");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Title)
                .HasConversion(v => v.Value, v => new RecipeTitle(v))
                .HasMaxLength(RecipeTitle.MaxLength).IsRequired();

            entity.Property(e => e.Description)
                .HasConversion(
                    v => v != null ? v.Value : null,
                    v => RecipeDescription.FromNullable(v))
                .HasMaxLength(RecipeDescription.MaxLength);

            entity.Property(e => e.Servings)
                .HasConversion(
                    v => v != null ? v.Value : (int?)null,
                    v => Servings.FromNullable(v));

            entity.Property(e => e.PreparationTime)
                .HasConversion(
                    v => v != null ? v.Value : (int?)null,
                    v => PreparationTime.FromNullable(v))
                .HasColumnName("PreparationTimeMinutes");

            entity.Property(e => e.CookingTime)
                .HasConversion(
                    v => v != null ? v.Value : (int?)null,
                    v => CookingTime.FromNullable(v))
                .HasColumnName("CookingTimeMinutes");

            entity.Property(e => e.Difficulty)
                .HasConversion(
                    v => v != null ? v.Value : null,
                    v => Difficulty.FromNullable(v))
                .HasMaxLength(Difficulty.MaxLength);

            entity.Property(e => e.ImageUrl)
                .HasConversion(
                    v => v != null ? v.Value : null,
                    v => ImageUrl.FromNullable(v))
                .HasMaxLength(ImageUrl.MaxLength);

            entity.Property(e => e.SourceUrl)
                .HasConversion(v => v.Value, v => new RecipeUrl(v))
                .HasMaxLength(RecipeUrl.MaxLength).IsRequired();

            entity.Property(e => e.Owner)
                .HasConversion(v => v.Value, v => new OwnerIdentifier(v))
                .HasMaxLength(OwnerIdentifier.MaxLength).IsRequired();

            entity.Property(e => e.ImportedAt).IsRequired();

            entity.OwnsMany(e => e.Ingredients, ingredient =>
            {
                ingredient.ToTable("RecipeIngredients");
                ingredient.WithOwner().HasForeignKey("RecipeId");
                ingredient.HasKey(nameof(Ingredient.Id));

                ingredient.Property(i => i.Name)
                    .HasConversion(v => v.Value, v => new IngredientName(v))
                    .HasMaxLength(IngredientName.MaxLength).IsRequired();

                ingredient.Property(i => i.Amount)
                    .HasConversion(
                        v => v != null ? v.Value : (decimal?)null,
                        v => Amount.FromNullable(v));

                ingredient.Property(i => i.Unit)
                    .HasConversion(
                        v => v != null ? v.Value : null,
                        v => Unit.FromNullable(v))
                    .HasMaxLength(Unit.MaxLength);
            });

            entity.OwnsMany(e => e.Steps, step =>
            {
                step.ToTable("RecipeSteps");
                step.WithOwner().HasForeignKey("RecipeId");
                step.HasKey(nameof(Step.Id));

                step.Property(s => s.Number)
                    .HasConversion(v => v.Value, v => new StepNumber(v))
                    .IsRequired();

                step.Property(s => s.Description)
                    .HasConversion(v => v.Value, v => new StepDescription(v))
                    .HasMaxLength(StepDescription.MaxLength).IsRequired();
            });

            entity.HasIndex(e => new { e.SourceUrl, e.Owner }).IsUnique();
            entity.Ignore(e => e.DomainEvents);
        });
    }
}
