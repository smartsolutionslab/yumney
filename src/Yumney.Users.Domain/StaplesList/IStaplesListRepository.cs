namespace SmartSolutionsLab.Yumney.Users.Domain.StaplesList;

public interface IStaplesListRepository
{
    Task<StaplesList?> FindByOwnerAsync(OwnerIdentifier owner, CancellationToken cancellationToken = default);

    // Tracked fetch for update flows. Throws EntityNotFoundException if not found.
    Task<StaplesList> GetByOwnerAsync(OwnerIdentifier owner, CancellationToken cancellationToken = default);

    Task AddAsync(StaplesList staplesList, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
