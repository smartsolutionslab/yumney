using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Domain.RecipeFavorite;
using SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence.Converters;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence.Configurations;

internal sealed class RecipeFavoriteConfiguration : IEntityTypeConfiguration<RecipeFavorite>
{
	public void Configure(EntityTypeBuilder<RecipeFavorite> entity)
	{
		entity.ToTable("RecipeFavorites");
		entity.HasKey(e => e.Id);
		entity.Property(e => e.Id)
			.HasConversion<RecipeFavoriteIdentifierConverter>();

		entity.Property(e => e.RecipeIdentifier)
			.HasConversion<RecipeIdentifierConverter>()
			.IsRequired();

		entity.Property(e => e.Owner)
			.HasConversion<RecipeOwnerIdentifierConverter>()
			.HasMaxLength(OwnerIdentifier.MaxLength)
			.IsRequired();

		entity.Property(e => e.FavoritedAt).IsRequired();

		entity.HasIndex(e => new { e.Owner, e.RecipeIdentifier }).IsUnique();
		entity.Ignore(e => e.DomainEvents);
	}
}
