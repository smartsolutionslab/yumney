using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence.Converters;

internal sealed class RecipeIdentifierConverter()
	: ValueConverter<RecipeIdentifier, Guid>(v => v.Value, v => RecipeIdentifier.From(v));
