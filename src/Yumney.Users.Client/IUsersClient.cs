namespace SmartSolutionsLab.Yumney.Users.Client;

public interface IUsersClient
{
	Task<IReadOnlyList<string>> GetMyStaplesAsync(CancellationToken cancellationToken = default);

	Task<UsersProfileResponse?> GetMyProfileAsync(CancellationToken cancellationToken = default);
}

public sealed record UsersProfileResponse(DietaryProfilePayload? DietaryProfile);

public sealed record DietaryProfilePayload(string? DietaryType, IReadOnlyList<string>? Restrictions);
