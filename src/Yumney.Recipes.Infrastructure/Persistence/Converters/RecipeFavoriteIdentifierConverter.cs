using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartSolutionsLab.Yumney.Recipes.Domain.RecipeFavorite;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence.Converters;

internal sealed class RecipeFavoriteIdentifierConverter()
    : ValueConverter<RecipeFavoriteIdentifier, Guid>(v => v.Value, v => RecipeFavoriteIdentifier.From(v));
