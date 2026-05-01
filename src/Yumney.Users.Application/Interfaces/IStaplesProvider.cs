using SmartSolutionsLab.Yumney.Users.Domain.StaplesList;

namespace SmartSolutionsLab.Yumney.Users.Application.Interfaces;

public interface IStaplesProvider
{
	Task<IReadOnlySet<string>> GetStapleNamesAsync(OwnerIdentifier owner, CancellationToken cancellationToken = default);
}
