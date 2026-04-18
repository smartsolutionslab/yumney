namespace SmartSolutionsLab.Yumney.Shared.Common;

public interface IUnitOfWork
{
	Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
