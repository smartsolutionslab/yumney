using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmartSolutionsLab.Yumney.Shared.AI;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.AI;

public class ChatClientHostBuilderExtensionsTests
{
	private const string OpenAiConnectionString = "Endpoint=https://api.openai.com/v1;Key=test-key;Model=gpt-test";
	private const string OllamaConnectionString = "Endpoint=http://localhost:11434;Model=mistral";

	[Fact]
	public void AddYumneyChatClient_OpenAiConnectionString_RegistersChatClient()
	{
		var builder = Builder(new() { ["ConnectionStrings:chat"] = OpenAiConnectionString, ["Llm:Provider"] = "OpenAI" });

		builder.AddYumneyChatClient();

		ChatClientOf(builder).Should().NotBeNull();
	}

	[Fact]
	public void AddYumneyChatClient_OllamaConnectionString_RegistersChatClient()
	{
		var builder = Builder(new() { ["ConnectionStrings:chat"] = OllamaConnectionString, ["Llm:Provider"] = "Ollama" });

		builder.AddYumneyChatClient();

		ChatClientOf(builder).Should().NotBeNull();
	}

	[Fact]
	public void AddYumneyChatClient_NoConnectionString_RegistersMissingChatClientReporter()
	{
		var builder = Builder([]);

		builder.AddYumneyChatClient();

		builder.Services.Should().Contain(descriptor => descriptor.ImplementationType == typeof(MissingChatClientReporter));
	}

	[Fact]
	public void AddYumneyChatClient_NoConnectionString_RegistersUnavailableChatClient()
	{
		var builder = Builder([]);

		builder.AddYumneyChatClient();

		ChatClientOf(builder).Should().BeOfType<UnavailableChatClient>();
	}

	[Fact]
	public async Task UnavailableChatClient_GetResponseAsync_Throws()
	{
		var builder = Builder([]);
		builder.AddYumneyChatClient();

		var act = () => ChatClientOf(builder)!.GetResponseAsync("hello");

		await act.Should().ThrowAsync<InvalidOperationException>();
	}

	[Fact]
	public void AddYumneyChatClient_E2ETests_RegistersNothing()
	{
		var builder = Builder(new() { ["E2ETests"] = "true", ["ConnectionStrings:chat"] = OpenAiConnectionString });
		var registrationsBefore = builder.Services.Count;

		builder.AddYumneyChatClient();

		builder.Services.Count.Should().Be(registrationsBefore);
	}

	[Fact]
	public void AddYumneyChatClient_CalledTwice_RegistersOneChatClient()
	{
		var builder = Builder(new() { ["ConnectionStrings:chat"] = OpenAiConnectionString });

		builder.AddYumneyChatClient();
		builder.AddYumneyChatClient();

		builder.Services.Count(descriptor => descriptor.ServiceType == typeof(IChatClient)).Should().Be(1);
	}

	private static HostApplicationBuilder Builder(Dictionary<string, string?> settings)
	{
		var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
		builder.Configuration.AddInMemoryCollection(settings);
		return builder;
	}

	private static IChatClient? ChatClientOf(HostApplicationBuilder builder) =>
		builder.Services.BuildServiceProvider().GetService<IChatClient>();
}
