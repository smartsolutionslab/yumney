using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Persistence;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence;

public sealed class RecipesDbContext(DbContextOptions<RecipesDbContext> options) : DbContext(options)
{
    public DbSet<Recipe> Recipes => Set<Recipe>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Recipe>(entity =>
        {
            entity.ToTable("Recipes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                .HasConversion(v => v.Value, v => new RecipeIdentifier(v));

            entity.Property(e => e.Title)
                .ConfigureRequiredStringValueObject(v => v.Value, v => new RecipeTitle(v), RecipeTitle.MaxLength);

            entity.Property(e => e.Description)
                .ConfigureNullableStringValueObject(v => v.Value, RecipeDescription.FromNullable, RecipeDescription.MaxLength);

            entity.Property(e => e.Servings)
                .ConfigureNullableIntValueObject(v => v.Value, Servings.FromNullable);

            entity.Property(e => e.PreparationTime)
                .ConfigureNullableIntValueObject(v => v.Value, PreparationTime.FromNullable)
                .HasColumnName("PreparationTimeMinutes");

            entity.Property(e => e.CookingTime)
                .ConfigureNullableIntValueObject(v => v.Value, CookingTime.FromNullable)
                .HasColumnName("CookingTimeMinutes");

            entity.Property(e => e.Difficulty)
                .ConfigureNullableStringValueObject(v => v.Value, Difficulty.FromNullable, Difficulty.MaxLength);

            entity.Property(e => e.ImageUrl)
                .ConfigureNullableStringValueObject(v => v.Value, ImageUrl.FromNullable, ImageUrl.MaxLength);

            entity.Property(e => e.Language)
                .ConfigureNullableStringValueObject(v => v.Value, RecipeLanguage.FromNullable, RecipeLanguage.MaxLength);

            entity.Property(e => e.SourceUrl)
                .ConfigureNullableStringValueObject(v => v.Value, RecipeUrl.FromNullable, RecipeUrl.MaxLength);

            entity.Property(e => e.Owner)
                .ConfigureRequiredStringValueObject(v => v.Value, v => new OwnerIdentifier(v), OwnerIdentifier.MaxLength);

            entity.Property(e => e.CreatedAt).IsRequired();

            entity.OwnsMany(e => e.Ingredients, ingredient =>
            {
                ingredient.ToTable("RecipeIngredients");
                ingredient.WithOwner().HasForeignKey("RecipeId");
                ingredient.HasKey(nameof(Ingredient.Id));

                ingredient.Property(i => i.Name)
                    .ConfigureRequiredStringValueObject(v => v.Value, v => new IngredientName(v), IngredientName.MaxLength);

                ingredient.Property(i => i.Amount)
                    .ConfigureNullableDecimalValueObject(v => v.Value, Amount.FromNullable);

                ingredient.Property(i => i.Unit)
                    .ConfigureNullableStringValueObject(v => v.Value, Unit.FromNullable, Unit.MaxLength);
            });

            entity.OwnsMany(e => e.Steps, step =>
            {
                step.ToTable("RecipeSteps");
                step.WithOwner().HasForeignKey("RecipeId");
                step.HasKey(nameof(Step.Id));

                step.Property(s => s.Number)
                    .ConfigureRequiredIntValueObject(v => v.Value, v => new StepNumber(v));

                step.Property(s => s.Description)
                    .ConfigureRequiredStringValueObject(v => v.Value, v => new StepDescription(v), StepDescription.MaxLength);
            });

            entity.HasIndex(e => e.Owner);
            entity.HasIndex(e => new { e.SourceUrl, e.Owner })
                .IsUnique()
                .HasFilter("\"SourceUrl\" IS NOT NULL");
            entity.Ignore(e => e.DomainEvents);
        });
    }
}
