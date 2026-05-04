using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Users.Domain.StaplesList;

namespace SmartSolutionsLab.Yumney.Users.Infrastructure.Persistence;

public sealed class StaplesListRepository(UsersDbContext context) : IStaplesListRepository
{
	private readonly DbSet<StaplesList> staplesLists = context.StaplesLists;

	public async Task<StaplesList?> FindByOwnerAsync(OwnerIdentifier owner, CancellationToken cancellationToken = default)
	{
		return await staplesLists
			.AsNoTracking()
			.Include(s => s.Items)
			.FirstOrDefaultAsync(s => s.Owner == owner, cancellationToken);
	}

	public async Task<StaplesList> GetByOwnerAsync(OwnerIdentifier owner, CancellationToken cancellationToken = default)
	{
		return await staplesLists
			.Include(s => s.Items)
			.FirstOrDefaultAsync(s => s.Owner == owner, cancellationToken)
			?? throw new EntityNotFoundException(nameof(StaplesList), owner.Value);
	}

	public async Task AddAsync(StaplesList staplesList, CancellationToken cancellationToken = default)
	{
		await staplesLists.AddAsync(staplesList, cancellationToken);
	}

	public async Task<int> DeleteByOwnerAsync(OwnerIdentifier owner, CancellationToken cancellationToken = default)
	{
		return await staplesLists.Where(list => list.Owner == owner).ExecuteDeleteAsync(cancellationToken);
	}
}
