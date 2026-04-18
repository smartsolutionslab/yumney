using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.Converters;

internal sealed class ShoppingListTitleConverter()
	: ValueConverter<ShoppingListTitle, string>(v => v.Value, v => ShoppingListTitle.From(v));
