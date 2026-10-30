using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace SmartSolutionsLab.Yumney.Shared.AI;

/// <summary>
/// Transitional: lets the Semantic Kernel chat service (tool calling) run on the
/// shared <see cref="IChatClient"/> until it moves to Microsoft.Extensions.AI
/// directly, after which this class and the Semantic Kernel dependency go away.
/// </summary>
public static class SemanticKernelBridge
{
	/// <summary>Register a <see cref="Kernel"/> whose chat completion is the host's <see cref="IChatClient"/>.</summary>
	/// <param name="services">The module's service collection.</param>
	/// <returns>The same collection, for fluent chaining.</returns>
	public static IServiceCollection AddKernelOnChatClient(this IServiceCollection services)
	{
		services.AddKernel();
		services.AddSingleton<IChatCompletionService>(serviceProvider =>
			serviceProvider.GetRequiredService<IChatClient>().AsKernelChatCompletion(serviceProvider));
		return services;
	}

	/// <summary>
	/// Adapt a chat client to Semantic Kernel. The provider connectors ran the
	/// tool-calling loop themselves; on top of an <see cref="IChatClient"/> the
	/// loop has to be added explicitly or <c>FunctionChoiceBehavior.Auto()</c>
	/// returns the tool call to the caller instead of invoking the kernel function.
	/// </summary>
	/// <param name="chatClient">The underlying chat client.</param>
	/// <param name="serviceProvider">Optional provider for logging.</param>
	/// <returns>A Semantic Kernel chat completion service with kernel function invocation.</returns>
	public static IChatCompletionService AsKernelChatCompletion(this IChatClient chatClient, IServiceProvider? serviceProvider = null) =>
		chatClient.AsBuilder()
			.UseKernelFunctionInvocation(serviceProvider?.GetService<ILoggerFactory>())
			.Build(serviceProvider)
			.AsChatCompletionService(serviceProvider);
}
