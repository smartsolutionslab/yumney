using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.Converters;

internal sealed class RecipeReferenceConverter()
	: ValueConverter<RecipeReference, Guid>(v => v.Value, v => RecipeReference.From(v));
