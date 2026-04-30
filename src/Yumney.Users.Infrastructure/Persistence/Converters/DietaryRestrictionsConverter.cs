using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

namespace SmartSolutionsLab.Yumney.Users.Infrastructure.Persistence.Converters;

internal sealed class DietaryRestrictionsConverter()
	: ValueConverter<IReadOnlyList<DietaryRestriction>, string>(
		restrictions => string.Join(',', restrictions.Select(restriction => restriction.Value)),
		serialized => ConvertFromString(serialized))
{
	private static IReadOnlyList<DietaryRestriction> ConvertFromString(string value) =>
		string.IsNullOrEmpty(value)
			? Array.Empty<DietaryRestriction>()
			: value.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(DietaryRestriction.From).ToList();
}
