using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartSolutionsLab.Yumney.Users.Domain.StaplesList;

namespace SmartSolutionsLab.Yumney.Users.Infrastructure.Persistence.Converters;

internal sealed class StaplesOwnerIdentifierConverter()
    : ValueConverter<OwnerIdentifier, string>(v => v.Value, v => OwnerIdentifier.From(v));
