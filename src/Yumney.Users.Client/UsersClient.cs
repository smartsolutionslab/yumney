using SmartSolutionsLab.Yumney.Shared.Web;

namespace SmartSolutionsLab.Yumney.Users.Client;

internal sealed class UsersClient(IModuleHttpClientFactory factory) : IUsersClient
{
	private readonly IModuleHttpClient http = factory.For("users-api");

	public Task<IReadOnlyList<string>> GetMyStaplesAsync(CancellationToken cancellationToken = default) =>
		http.GetOrDefaultAsync<IReadOnlyList<string>>(
			"/api/v1/users/staples",
			[],
			"GetStaples",
			cancellationToken);

	public Task<UsersProfileResponse?> GetMyProfileAsync(CancellationToken cancellationToken = default) =>
		http.FindAsync<UsersProfileResponse>(
			"/api/v1/users/me/profile",
			"GetUserProfile",
			cancellationToken);
}
