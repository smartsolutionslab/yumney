using System;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Shopping.Outbox;

// Validates that the ShoppingList aggregate's save path actually goes through
// the Wolverine transactional outbox by asserting on the eventual read-model
// state populated by the projection handler.
//
// What this proves:
//   1. ShoppingListEventStore.SaveAsync stages the integration event on
//      IDbContextOutbox<ShoppingDbContext> and commits it together with the
//      event-store rows in one Postgres transaction (otherwise the
//      ShoppingListCreatedModuleEvent would never reach the relay).
//   2. Wolverine's relay drains the outbox row and delivers via RabbitMQ.
//   3. ShoppingListProjection consumes the message and writes both
//      ShoppingListSummaryReadItem and ShoppingListItemReadItem rows.
//
// What this does NOT cover (deferred — requires control over the RabbitMQ
// resource container that AspireFixture does not currently expose):
//   - Mid-flight RabbitMQ outage with retry-after-recovery. Wolverine's
//     in-process retry behaviour is unit-tested upstream; this suite would
//     need to stop/restart the messaging container between SaveAsync and
//     the assertion to exercise that path against our composition.
[Collection(AspireCollection.Name)]
public class OutboxDeliveryTests(AspireFixture fixture)
{
	private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

	[Fact]
	public async Task ShoppingListSave_PersistsBothReadModelRowsViaOutbox()
	{
		using var client = await fixture.CreateAuthenticatedClientAsync("shopping-api");

		var createRequest = new
		{
			title = "Outbox Delivery Test",
			items = new object[]
			{
				new { name = "Pasta", amount = 500m, unit = "g" },
				new { name = "Tomatoes", amount = 4m },
			},
		};

		var createResponse = await client.PostAsJsonAsync("/api/v1/shopping-lists", createRequest);
		createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

		var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
		var listId = created.GetProperty("identifier").GetGuid();

		// Read directly from the DB (not via the API GET) so we assert on the
		// projection's actual writes — catches divergences where the API reads
		// from a different source than the projection writes to.
		await Eventually.AssertAsync(async () =>
		{
			await using var db = await fixture.CreateShoppingDbContextAsync();

			var summary = await db.Set<ShoppingListSummaryReadItem>()
				.AsNoTracking()
				.FirstOrDefaultAsync(s => s.Id == listId);

			summary.Should().NotBeNull("the ShoppingListCreated integration event should have flowed " +
				"through the outbox + Wolverine relay and triggered the projection.");
			summary!.Title.Should().Be("Outbox Delivery Test");
			summary.ItemCount.Should().Be(2);

			var itemRows = await db.Set<ShoppingListItemReadItem>()
				.AsNoTracking()
				.Where(item => item.ListId == listId)
				.OrderBy(item => item.Name)
				.ToListAsync();

			itemRows.Should().HaveCount(2);
			itemRows[0].Name.Should().Be("Pasta");
			itemRows[1].Name.Should().Be("Tomatoes");
		});
	}
}
