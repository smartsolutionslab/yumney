using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Web;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Web;

public class ModuleHttpClientTests
{
	[Fact]
	public async Task GetOrDefaultAsync_Success_ReturnsBody()
	{
		var http = CreateClient(new StubHandler(HttpStatusCode.OK, """{ "name": "ok" }"""));

		var result = await http.GetOrDefaultAsync("/anything", new Payload("fallback"), "Op");

		result.Name.Should().Be("ok");
	}

	[Fact]
	public async Task GetOrDefaultAsync_NonSuccess_ReturnsFallback()
	{
		var http = CreateClient(new StubHandler(HttpStatusCode.InternalServerError));

		var result = await http.GetOrDefaultAsync("/anything", new Payload("fallback"), "Op");

		result.Name.Should().Be("fallback");
	}

	[Fact]
	public async Task GetOrDefaultAsync_NetworkException_ReturnsFallback()
	{
		var http = CreateClient(new ThrowingHandler(new HttpRequestException("boom")));

		var result = await http.GetOrDefaultAsync("/anything", new Payload("fallback"), "Op");

		result.Name.Should().Be("fallback");
	}

	[Fact]
	public async Task GetOrDefaultAsync_Cancelled_Rethrows()
	{
		var http = CreateClient(new ThrowingHandler(new OperationCanceledException()));
		using var cts = new CancellationTokenSource();
		cts.Cancel();

		var act = async () => await http.GetOrDefaultAsync("/anything", new Payload("fallback"), "Op", cts.Token);

		await act.Should().ThrowAsync<OperationCanceledException>();
	}

	[Fact]
	public async Task FindAsync_Success_ReturnsBody()
	{
		var http = CreateClient(new StubHandler(HttpStatusCode.OK, """{ "name": "found" }"""));

		var result = await http.FindAsync<Payload>("/anything", "Op");

		result!.Name.Should().Be("found");
	}

	[Fact]
	public async Task FindAsync_NotFound_ReturnsNull()
	{
		var http = CreateClient(new StubHandler(HttpStatusCode.NotFound));

		var result = await http.FindAsync<Payload>("/anything", "Op");

		result.Should().BeNull();
	}

	[Fact]
	public async Task FindAsync_NonSuccess_ReturnsNull()
	{
		var http = CreateClient(new StubHandler(HttpStatusCode.InternalServerError));

		var result = await http.FindAsync<Payload>("/anything", "Op");

		result.Should().BeNull();
	}

	[Fact]
	public async Task FindAsync_Cancelled_Rethrows()
	{
		var http = CreateClient(new ThrowingHandler(new OperationCanceledException()));
		using var cts = new CancellationTokenSource();
		cts.Cancel();

		var act = async () => await http.FindAsync<Payload>("/anything", "Op", cts.Token);

		await act.Should().ThrowAsync<OperationCanceledException>();
	}

	[Fact]
	public async Task PostAsync_Success_DoesNotThrow()
	{
		var http = CreateClient(new StubHandler(HttpStatusCode.NoContent));

		var act = async () => await http.PostAsync("/anything", new Payload("body"), "Op");

		await act.Should().NotThrowAsync();
	}

	[Fact]
	public async Task PostAsync_NonSuccess_Throws()
	{
		var http = CreateClient(new StubHandler(HttpStatusCode.InternalServerError));

		var act = async () => await http.PostAsync("/anything", new Payload("body"), "Op");

		await act.Should().ThrowAsync<HttpRequestException>();
	}

	[Fact]
	public async Task PostAsync_Cancelled_Rethrows()
	{
		var http = CreateClient(new ThrowingHandler(new OperationCanceledException()));
		using var cts = new CancellationTokenSource();
		cts.Cancel();

		var act = async () => await http.PostAsync("/anything", new Payload("body"), "Op", cts.Token);

		await act.Should().ThrowAsync<OperationCanceledException>();
	}

	private static ModuleHttpClient CreateClient(HttpMessageHandler handler) =>
		new(
			new HttpClient(handler) { BaseAddress = new Uri("http://test") },
			"test-upstream",
			NullLogger<ModuleHttpClient>.Instance);

	private sealed record Payload(string Name);

	private sealed class StubHandler(HttpStatusCode status, string? body = null) : HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var response = new HttpResponseMessage(status);
			if (body is not null)
			{
				response.Content = new StringContent(body, Encoding.UTF8, "application/json");
			}

			return Task.FromResult(response);
		}
	}

	private sealed class ThrowingHandler(Exception exception) : HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
			throw exception;
	}
}
