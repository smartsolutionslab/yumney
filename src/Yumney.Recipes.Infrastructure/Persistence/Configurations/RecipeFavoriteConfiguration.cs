using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Domain.RecipeFavorite;
using SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence.Converters;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence.Configurations;

internal sealed class RecipeFavoriteConfiguration : IEntityTypeConfiguration<RecipeFavorite>
{
	public void Configure(EntityTypeBuilder<RecipeFavorite> builder)
	{
		builder.ToTable("RecipeFavorites");
		builder.HasKey(favorite => favorite.Id);
		builder.Property(favorite => favorite.Id)
			.HasConversion<RecipeFavoriteIdentifierConverter>();

		builder.Property(favorite => favorite.Recipe)
			.HasConversion<RecipeIdentifierConverter>()
			.HasColumnName("RecipeIdentifier")
			.IsRequired();

		builder.Property(favorite => favorite.Owner)
			.HasConversion<RecipeOwnerIdentifierConverter>()
			.HasMaxLength(OwnerIdentifier.MaxLength)
			.IsRequired();

		builder.Property(favorite => favorite.FavoritedAt).IsRequired();

		builder.HasIndex(favorite => new { favorite.Owner, favorite.Recipe })
			.IsUnique()
			.HasDatabaseName("IX_RecipeFavorites_Owner_RecipeIdentifier");
		builder.Ignore(favorite => favorite.DomainEvents);
	}
}
