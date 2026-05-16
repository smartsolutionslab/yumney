using System.Text.Json.Nodes;

namespace SmartSolutionsLab.Yumney.Mcp.Server.Tests.OpenApi.TestSupport;

internal static class JsonObjectExtensions
{
	// FluentAssertions doesn't ship JsonObject-shaped dictionary assertions;
	// JsonObject implements IDictionary<string, JsonNode?>, so a cast unlocks
	// the regular dictionary assertions (ContainKey / HaveCount / etc).
	public static IDictionary<string, JsonNode?> AsDictionary(this JsonObject obj) => obj;
}
