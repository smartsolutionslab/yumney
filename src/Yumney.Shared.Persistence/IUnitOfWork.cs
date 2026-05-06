namespace SmartSolutionsLab.Yumney.Shared.Persistence;

public interface IUnitOfWork
{
	Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
