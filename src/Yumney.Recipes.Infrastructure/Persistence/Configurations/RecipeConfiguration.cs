using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence.Converters;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence.Configurations;

internal sealed class RecipeConfiguration : IEntityTypeConfiguration<Recipe>
{
	public void Configure(EntityTypeBuilder<Recipe> builder)
	{
		builder.ToTable("Recipes");
		builder.HasKey(recipe => recipe.Id);
		builder.Property(recipe => recipe.Id)
			.HasConversion<RecipeIdentifierConverter>();

		builder.Property(recipe => recipe.Title)
			.HasConversion<RecipeTitleConverter>()
			.HasMaxLength(RecipeTitle.MaxLength)
			.IsRequired();

		builder.Property(recipe => recipe.Description)
			.HasConversion<RecipeDescriptionConverter>()
			.HasMaxLength(RecipeDescription.MaxLength);

		builder.Property(recipe => recipe.Servings)
			.HasConversion<ServingsConverter>();

		builder.OwnsOne(recipe => recipe.Timing, timing =>
		{
			timing.Property(slot => slot.Preparation)
				.HasConversion<PreparationTimeConverter>()
				.HasColumnName("PreparationTimeMinutes");

			timing.Property(slot => slot.Cooking)
				.HasConversion<CookingTimeConverter>()
				.HasColumnName("CookingTimeMinutes");
		});

		builder.Property(recipe => recipe.Difficulty)
			.HasConversion<DifficultyConverter>()
			.HasMaxLength(Difficulty.MaxLength);

		builder.Property(recipe => recipe.ImageUrl)
			.HasConversion<ImageUrlConverter>()
			.HasMaxLength(ImageUrl.MaxLength);

		builder.Property(recipe => recipe.Language)
			.HasConversion<RecipeLanguageConverter>()
			.HasMaxLength(RecipeLanguage.MaxLength);

		builder.Property(recipe => recipe.SourceUrl)
			.HasConversion<RecipeUrlConverter>()
			.HasMaxLength(RecipeUrl.MaxLength);

		builder.Property(recipe => recipe.Owner)
			.HasConversion<RecipeOwnerIdentifierConverter>()
			.HasMaxLength(OwnerIdentifier.MaxLength)
			.IsRequired();

		builder.Property(recipe => recipe.CreatedAt).IsRequired();

		builder.Property(recipe => recipe.Rating)
			.HasConversion<RatingConverter>();

		builder.Property(recipe => recipe.Notes)
			.HasConversion<NotesConverter>()
			.HasMaxLength(Notes.MaxLength);

		builder.OwnsMany(recipe => recipe.Ingredients, ingredient =>
		{
			ingredient.ToTable("RecipeIngredients");
			ingredient.WithOwner().HasForeignKey("RecipeId");
			ingredient.HasKey(nameof(Ingredient.Id));
			ingredient.Property(row => row.Id)
				.HasConversion<IngredientIdentifierConverter>();

			ingredient.Property(row => row.Name)
				.HasConversion<IngredientNameConverter>()
				.HasMaxLength(IngredientName.MaxLength)
				.IsRequired();

			ingredient.OwnsOne(row => row.Quantity, quantity =>
			{
				quantity.Property(value => value.Amount)
					.HasConversion<RecipeAmountConverter>()
					.HasColumnName("Amount");

				quantity.Property(value => value.Unit)
					.HasConversion<RecipeUnitConverter>()
					.HasMaxLength(Unit.MaxLength)
					.HasColumnName("Unit");
			});
		});

		builder.OwnsMany(recipe => recipe.Steps, step =>
		{
			step.ToTable("RecipeSteps");
			step.WithOwner().HasForeignKey("RecipeId");
			step.HasKey(nameof(Step.Id));
			step.Property(row => row.Id)
				.HasConversion<StepIdentifierConverter>();

			step.Property(row => row.Number)
				.HasConversion<StepNumberConverter>()
				.IsRequired();

			step.Property(row => row.Description)
				.HasConversion<StepDescriptionConverter>()
				.HasMaxLength(StepDescription.MaxLength)
				.IsRequired();
		});

		builder.OwnsMany(recipe => recipe.Tags, tag =>
		{
			tag.ToTable("RecipeTags");
			tag.WithOwner().HasForeignKey("RecipeId");
			tag.Property(row => row.Value)
				.HasColumnName("Tag")
				.HasMaxLength(RecipeTag.MaxLength);
		});

		builder.HasIndex(recipe => recipe.Owner);
		builder.HasIndex(recipe => new { recipe.Owner, recipe.CreatedAt });
		builder.HasIndex(recipe => new { recipe.Owner, recipe.Title });
		builder.HasIndex(recipe => new { recipe.SourceUrl, recipe.Owner })
			.IsUnique()
			.HasFilter("\"SourceUrl\" IS NOT NULL");
		builder.Property<uint>("xmin")
			.HasColumnType("xid")
			.IsRowVersion();
		builder.Ignore(recipe => recipe.DomainEvents);
	}
}
