using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.Converters;

internal sealed class ShoppingListIdentifierConverter()
    : ValueConverter<ShoppingListIdentifier, Guid>(v => v.Value, v => ShoppingListIdentifier.From(v));
