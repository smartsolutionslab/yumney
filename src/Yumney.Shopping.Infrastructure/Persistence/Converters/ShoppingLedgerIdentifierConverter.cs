using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.Converters;

internal sealed class ShoppingLedgerIdentifierConverter()
    : ValueConverter<ShoppingLedgerIdentifier, Guid>(v => v.Value, v => ShoppingLedgerIdentifier.From(v));
