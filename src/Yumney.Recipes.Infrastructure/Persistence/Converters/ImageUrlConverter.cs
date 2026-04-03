using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence.Converters;

internal sealed class ImageUrlConverter()
    : ValueConverter<ImageUrl, string>(v => v.Value, v => ImageUrl.From(v));
