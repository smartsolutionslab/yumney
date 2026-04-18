using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartSolutionsLab.Yumney.Users.Domain.StaplesList;

namespace SmartSolutionsLab.Yumney.Users.Infrastructure.Persistence.Converters;

internal sealed class StaplesListIdentifierConverter()
	: ValueConverter<StaplesListIdentifier, Guid>(v => v.Value, v => StaplesListIdentifier.From(v));
