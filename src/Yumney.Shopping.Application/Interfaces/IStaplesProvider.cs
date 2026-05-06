namespace SmartSolutionsLab.Yumney.Shopping.Application.Interfaces;

public interface IStaplesProvider
{
	Task<IReadOnlySet<string>> GetStapleNamesAsync(CancellationToken cancellationToken = default);
}
