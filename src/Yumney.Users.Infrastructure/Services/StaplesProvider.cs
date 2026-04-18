using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Users.Domain.StaplesList;

namespace SmartSolutionsLab.Yumney.Users.Infrastructure.Services;

public sealed class StaplesProvider(IStaplesListRepository staplesLists) : IStaplesProvider
{
	public async Task<IReadOnlySet<string>> GetStapleNamesAsync(
		string ownerId,
		CancellationToken cancellationToken = default)
	{
		var owner = OwnerIdentifier.From(ownerId);
		var staples = await staplesLists.FindByOwnerAsync(owner, cancellationToken);

		if (staples is null)
		{
			return StaplesList.DefaultItems
				.Select(i => i.Value.ToLowerInvariant())
				.ToHashSet(StringComparer.OrdinalIgnoreCase);
		}

		return staples.Items
			.Select(i => i.Value.ToLowerInvariant())
			.ToHashSet(StringComparer.OrdinalIgnoreCase);
	}
}
