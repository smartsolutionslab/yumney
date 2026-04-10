using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.Converters;

internal sealed class TransactionSourceConverter()
    : ValueConverter<TransactionSource, string>(v => v.Value, v => TransactionSource.From(v));
