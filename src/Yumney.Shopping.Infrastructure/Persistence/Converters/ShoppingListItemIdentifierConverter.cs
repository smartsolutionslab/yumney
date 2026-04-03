using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.Converters;

internal sealed class ShoppingListItemIdentifierConverter()
    : ValueConverter<ShoppingListItemIdentifier, Guid>(v => v.Value, v => ShoppingListItemIdentifier.From(v));
