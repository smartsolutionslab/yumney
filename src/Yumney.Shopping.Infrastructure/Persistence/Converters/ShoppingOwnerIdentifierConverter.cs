using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.Converters;

internal sealed class ShoppingOwnerIdentifierConverter()
	: ValueConverter<OwnerIdentifier, string>(v => v.Value, v => OwnerIdentifier.From(v));
