using Aspire.Hosting.JavaScript;
using Aspire.Hosting.Yarp;
using SmartSolutionsLab.Yumney.AppHost.Options;
using Yarp.ReverseProxy.Forwarder;

namespace SmartSolutionsLab.Yumney.AppHost;

internal static class FrontendComposition
{
	// Wires the frontend layer for `aspire run` (incl. E2E): four MFEs +
	// the Yumney.Gateway project. Sets the MCP public-URL env var so
	// in-browser OIDC discovery hits the gateway origin even though YARP
	// rewrites the Host header.
	public static IDistributedApplicationBuilder AddRunModeFrontend(
		this IDistributedApplicationBuilder builder,
		AppHostOptions options,
		IResourceBuilder<ProjectResource> recipesApi,
		IResourceBuilder<ProjectResource> shoppingApi,
		IResourceBuilder<ProjectResource> usersApi,
		IResourceBuilder<ProjectResource> mealplanApi,
		IResourceBuilder<ProjectResource> mcpServer,
		IResourceBuilder<KeycloakResource> keycloak)
	{
		var shell = AddMfes(builder, options);

		var gateway = builder.AddProject<Projects.Yumney_Gateway>("yumney-gateway")
			.WithHttpEndpoint(port: 5100)
			.WithReference(recipesApi)
			.WithReference(shoppingApi)
			.WithReference(usersApi)
			.WithReference(mealplanApi)
			.WithReference(mcpServer)
			.WithReference(shell)
			.WithReference(keycloak)
			.WaitFor(keycloak);

		// MCP's OAuth challenge advertises the public discovery URL via
		// resource_metadata. Without this env var it would build the URL
		// from the inbound request's Host header — which YARP rewrites to
		// the MCP container's internal DNS name, so Claude/ChatGPT can't
		// reach it.
		mcpServer.WithEnvironment("McpServer__PublicUrl", ReferenceExpression.Create($"{gateway.GetEndpoint("http")}/mcp"));

		return builder;
	}

	// Wires the frontend layer for publish mode (Azure Container Apps): a
	// Dockerfile-built static-site container + a YARP gateway that routes
	// each module's API plus the SPA catch-all and the OAuth-protected-
	// resource well-known path.
	public static IDistributedApplicationBuilder AddPublishModeFrontend(
		this IDistributedApplicationBuilder builder,
		IResourceBuilder<ProjectResource> recipesApi,
		IResourceBuilder<ProjectResource> shoppingApi,
		IResourceBuilder<ProjectResource> usersApi,
		IResourceBuilder<ProjectResource> mealplanApi,
		IResourceBuilder<ProjectResource> mcpServer)
	{
		var frontend = builder
			.AddDockerfile("yumney-frontend", "../../client", "docker/Dockerfile")
			.WithHttpEndpoint(targetPort: 80);

		var gateway = builder
			.AddYarp("yumney-gateway")
			.WithConfiguration(yarp =>
			{
				yarp.AddRoute("/api/v1/recipes/{**catch-all}", recipesApi);
				yarp.AddRoute("/api/v1/shopping-lists/{**catch-all}", shoppingApi);
				yarp.AddRoute("/api/v1/auth/{**catch-all}", usersApi);
				yarp.AddRoute("/api/v1/users/{**catch-all}", usersApi);
				yarp.AddRoute("/api/v1/meal-plans/{**catch-all}", mealplanApi);
				var mcpCluster = yarp.AddCluster(mcpServer);

				// MCP SSE: extend the 100 s ActivityTimeout so idle sessions survive; buffering pinned off.
				mcpCluster.WithForwarderRequestConfig(new ForwarderRequestConfig
				{
					ActivityTimeout = TimeSpan.FromMinutes(10),
					AllowResponseBuffering = false,
				});
				yarp.AddRoute("/mcp/{**catch-all}", mcpCluster);

				// RFC 9728 mandates the protected-resource metadata document lives
				// at the origin's well-known path. Route it to MCP so Claude/ChatGPT
				// can fetch it through the public gateway (otherwise it falls
				// through to the SPA catch-all below).
				yarp.AddRoute("/.well-known/oauth-protected-resource", mcpCluster);

				// MCP server's anonymous discovery endpoints — without these
				// explicit routes both fall through to the SPA catch-all and
				// return the frontend HTML instead of the JSON manifests that
				// the MCP setup docs advertise (Claude.ai/ChatGPT custom-GPT
				// setup fetch these to enumerate the tool surface).
				yarp.AddRoute("/discovered-capabilities", mcpCluster);
				yarp.AddRoute("/openapi/v1.json", mcpCluster);
				yarp.AddRoute("/{**catch-all}", frontend.GetEndpoint("http"));
			})
			.WithExternalHttpEndpoints();

		// See run-mode comment in AddRunModeFrontend. In publish mode the
		// gateway endpoint resolves to the externally-published Container
		// Apps URL, so MCP's OAuth discovery advertises a URL Claude can
		// actually fetch.
		mcpServer.WithEnvironment("McpServer__PublicUrl", ReferenceExpression.Create($"{gateway.GetEndpoint("http")}/mcp"));

		frontend.PublishAsScaledContainerApp(min: 1, max: 5, concurrentRequests: 100);

		return builder;
	}

	private static IResourceBuilder<JavaScriptAppResource> AddMfes(IDistributedApplicationBuilder builder, AppHostOptions options)
	{
		var addMfe = (string name, string script, int port) =>
#pragma warning disable ASPIREBROWSERLOGS001
			builder.AddJavaScriptApp(name, "../../client", script)
				.WithYarn()
				.WithEnvironment("NX_DAEMON", "false")
				.WithEnvironment("NX_ISOLATE_PLUGINS", "false")
				.WithHttpEndpoint(targetPort: port)
				.WithBrowserLogs();
#pragma warning restore ASPIREBROWSERLOGS001

		// E2E mode runs against a production build (`yarn build:all`
		// co-locates every MFE in `dist/apps/shell/browser`) so PWA tests
		// exercise the real shape.
		if (options.E2ETests)
		{
			return addMfe("shell", "serve:shell:dist", 4200);
		}

		var shell = addMfe("shell", "serve:shell", 4200);
		addMfe("recipes-mfe", "serve:recipes", 4201);
		addMfe("shopping-mfe", "serve:shopping", 4202);
		addMfe("account-mfe", "serve:account", 4203);
		return shell;
	}
}
