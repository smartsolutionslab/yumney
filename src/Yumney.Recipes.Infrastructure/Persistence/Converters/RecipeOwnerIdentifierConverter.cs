using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence.Converters;

internal sealed class RecipeOwnerIdentifierConverter()
    : ValueConverter<OwnerIdentifier, string>(v => v.Value, v => OwnerIdentifier.From(v));
