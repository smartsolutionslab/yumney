using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence.Converters;

internal sealed class IngredientIdentifierConverter()
	: ValueConverter<IngredientIdentifier, Guid>(v => v.Value, v => IngredientIdentifier.From(v));
