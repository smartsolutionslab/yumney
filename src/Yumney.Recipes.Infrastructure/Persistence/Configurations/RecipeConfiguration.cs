using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence.Configurations;

internal sealed class RecipeConfiguration : IEntityTypeConfiguration<Recipe>
{
    public void Configure(EntityTypeBuilder<Recipe> entity)
    {
        entity.ToTable("Recipes");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id)
            .HasConversion(v => v.Value, v => RecipeIdentifier.From(v));

        entity.Property(e => e.Title)
            .HasConversion(v => v.Value, v => RecipeTitle.From(v))
            .HasMaxLength(RecipeTitle.MaxLength)
            .IsRequired();

        entity.Property(e => e.Description)
            .HasConversion(v => v != null ? v.Value : null, v => RecipeDescription.FromNullable(v))
            .HasMaxLength(RecipeDescription.MaxLength);

        entity.Property(e => e.Servings)
            .HasConversion(v => v != null ? v.Value : (int?)null, v => Servings.FromNullable(v));

        entity.Property(e => e.PreparationTime)
            .HasConversion(v => v != null ? v.Value : (int?)null, v => PreparationTime.FromNullable(v))
            .HasColumnName("PreparationTimeMinutes");

        entity.Property(e => e.CookingTime)
            .HasConversion(v => v != null ? v.Value : (int?)null, v => CookingTime.FromNullable(v))
            .HasColumnName("CookingTimeMinutes");

        entity.Property(e => e.Difficulty)
            .HasConversion(v => v != null ? v.Value : null, v => Difficulty.FromNullable(v))
            .HasMaxLength(Difficulty.MaxLength);

        entity.Property(e => e.ImageUrl)
            .HasConversion(v => v != null ? v.Value : null, v => ImageUrl.FromNullable(v))
            .HasMaxLength(ImageUrl.MaxLength);

        entity.Property(e => e.Language)
            .HasConversion(v => v != null ? v.Value : null, v => RecipeLanguage.FromNullable(v))
            .HasMaxLength(RecipeLanguage.MaxLength);

        entity.Property(e => e.SourceUrl)
            .HasConversion(v => v != null ? v.Value : null, v => RecipeUrl.FromNullable(v))
            .HasMaxLength(RecipeUrl.MaxLength);

        entity.Property(e => e.Owner)
            .HasConversion(v => v.Value, v => OwnerIdentifier.From(v))
            .HasMaxLength(OwnerIdentifier.MaxLength)
            .IsRequired();

        entity.Property(e => e.CreatedAt).IsRequired();

        entity.OwnsMany(e => e.Ingredients, ingredient =>
        {
            ingredient.ToTable("RecipeIngredients");
            ingredient.WithOwner().HasForeignKey("RecipeId");
            ingredient.HasKey(nameof(Ingredient.Id));
            ingredient.Property(i => i.Id)
                .HasConversion(v => v.Value, v => IngredientIdentifier.From(v));

            ingredient.Property(i => i.Name)
                .HasConversion(v => v.Value, v => IngredientName.From(v))
                .HasMaxLength(IngredientName.MaxLength)
                .IsRequired();

            ingredient.OwnsOne(i => i.Quantity, q =>
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

        entity.OwnsMany(e => e.Steps, step =>
        {
            step.ToTable("RecipeSteps");
            step.WithOwner().HasForeignKey("RecipeId");
            step.HasKey(nameof(Step.Id));
            step.Property(s => s.Id)
                .HasConversion(v => v.Value, v => StepIdentifier.From(v));

            step.Property(s => s.Number)
                .HasConversion(v => v.Value, v => StepNumber.From(v))
                .IsRequired();

            step.Property(s => s.Description)
                .HasConversion(v => v.Value, v => StepDescription.From(v))
                .HasMaxLength(StepDescription.MaxLength)
                .IsRequired();
        });

        entity.OwnsMany(e => e.Tags, tag =>
        {
            tag.ToTable("RecipeTags");
            tag.WithOwner().HasForeignKey("RecipeId");
            tag.Property(t => t.Value)
                .HasColumnName("Tag")
                .HasMaxLength(RecipeTag.MaxLength);
        });

        entity.HasIndex(e => e.Owner);
        entity.HasIndex(e => new { e.Owner, e.CreatedAt });
        entity.HasIndex(e => new { e.Owner, e.Title });
        entity.HasIndex(e => new { e.SourceUrl, e.Owner })
            .IsUnique()
            .HasFilter("\"SourceUrl\" IS NOT NULL");
        entity.Property<uint>("xmin")
            .HasColumnType("xid")
            .IsRowVersion();
        entity.Ignore(e => e.DomainEvents);
    }
}
