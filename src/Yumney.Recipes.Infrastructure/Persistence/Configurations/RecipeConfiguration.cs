using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence.Converters;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence.Configurations;

internal sealed class RecipeConfiguration : IEntityTypeConfiguration<Recipe>
{
	public void Configure(EntityTypeBuilder<Recipe> entity)
	{
		entity.ToTable("Recipes");
		entity.HasKey(e => e.Id);
		entity.Property(e => e.Id)
			.HasConversion<RecipeIdentifierConverter>();

		entity.Property(e => e.Title)
			.HasConversion<RecipeTitleConverter>()
			.HasMaxLength(RecipeTitle.MaxLength)
			.IsRequired();

		entity.Property(e => e.Description)
			.HasConversion<RecipeDescriptionConverter>()
			.HasMaxLength(RecipeDescription.MaxLength);

		entity.Property(e => e.Servings)
			.HasConversion<ServingsConverter>();

		entity.OwnsOne(e => e.Timing, timing =>
		{
			timing.Property(t => t.Preparation)
				.HasConversion<PreparationTimeConverter>()
				.HasColumnName("PreparationTimeMinutes");

			timing.Property(t => t.Cooking)
				.HasConversion<CookingTimeConverter>()
				.HasColumnName("CookingTimeMinutes");
		});

		entity.Property(e => e.Difficulty)
			.HasConversion<DifficultyConverter>()
			.HasMaxLength(Difficulty.MaxLength);

		entity.Property(e => e.ImageUrl)
			.HasConversion<ImageUrlConverter>()
			.HasMaxLength(ImageUrl.MaxLength);

		entity.Property(e => e.Language)
			.HasConversion<RecipeLanguageConverter>()
			.HasMaxLength(RecipeLanguage.MaxLength);

		entity.Property(e => e.SourceUrl)
			.HasConversion<RecipeUrlConverter>()
			.HasMaxLength(RecipeUrl.MaxLength);

		entity.Property(e => e.Owner)
			.HasConversion<RecipeOwnerIdentifierConverter>()
			.HasMaxLength(OwnerIdentifier.MaxLength)
			.IsRequired();

		entity.Property(e => e.CreatedAt).IsRequired();

		entity.OwnsMany(e => e.Ingredients, ingredient =>
		{
			ingredient.ToTable("RecipeIngredients");
			ingredient.WithOwner().HasForeignKey("RecipeId");
			ingredient.HasKey(nameof(Ingredient.Id));
			ingredient.Property(i => i.Id)
				.HasConversion<IngredientIdentifierConverter>();

			ingredient.Property(i => i.Name)
				.HasConversion<IngredientNameConverter>()
				.HasMaxLength(IngredientName.MaxLength)
				.IsRequired();

			ingredient.OwnsOne(i => i.Quantity, q =>
			{
				q.Property(x => x.Amount)
					.HasConversion<RecipeAmountConverter>()
					.HasColumnName("Amount");

				q.Property(x => x.Unit)
					.HasConversion<RecipeUnitConverter>()
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
				.HasConversion<StepIdentifierConverter>();

			step.Property(s => s.Number)
				.HasConversion<StepNumberConverter>()
				.IsRequired();

			step.Property(s => s.Description)
				.HasConversion<StepDescriptionConverter>()
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
