using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.Converters;

internal sealed class LedgerTransactionIdentifierConverter()
    : ValueConverter<LedgerTransactionIdentifier, Guid>(v => v.Value, v => LedgerTransactionIdentifier.From(v));
