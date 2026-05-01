using SmartSolutionsLab.Yumney.Users.Application.Interfaces;
using SmartSolutionsLab.Yumney.Users.Domain.StaplesList;

namespace SmartSolutionsLab.Yumney.Users.Infrastructure.Services;

public sealed class StaplesProvider(IStaplesListRepository staplesLists) : IStaplesProvider
{
	public async Task<IReadOnlySet<string>> GetStapleNamesAsync(
		OwnerIdentifier owner,
		CancellationToken cancellationToken = default)
	{
		var staples = await staplesLists.FindByOwnerAsync(owner, cancellationToken);

		if (staples is null)
		{
			return StaplesList.DefaultItems
				.Select(item => item.Value.ToLowerInvariant())
				.ToHashSet(StringComparer.OrdinalIgnoreCase);
		}

		return staples.Items
			.Select(item => item.Value.ToLowerInvariant())
			.ToHashSet(StringComparer.OrdinalIgnoreCase);
	}
}
