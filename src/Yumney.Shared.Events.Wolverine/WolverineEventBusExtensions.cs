using System.Reflection;
using JasperFx;
using JasperFx.Resources;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Guards;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Persistence;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;

namespace SmartSolutionsLab.Yumney.Shared.Events.Wolverine;

/// <summary>
/// Extension methods for registering Wolverine-based event bus with RabbitMQ transport
/// and the PostgreSQL-backed transactional outbox.
/// </summary>
public static class WolverineEventBusExtensions
{
	/// <summary>
	/// Registers Wolverine with RabbitMQ as the integration event bus and a
	/// PostgreSQL-backed durable outbox so events written by event-sourced
	/// stores share a transactional fate with their owning DbContext save.
	/// Domain events stay in-process via <see cref="InProcessDomainEventDispatcher"/>.
	/// </summary>
	/// <param name="builder">The host application builder.</param>
	/// <param name="outboxConnectionName">Connection-string name (per-module Postgres) backing the outbox.</param>
	/// <param name="outboxSchema">PostgreSQL schema that holds the Wolverine envelope tables. One schema per module DB.</param>
	/// <param name="eventHandlerAssemblies">Assemblies to scan for IIntegrationEventHandler / IModuleEventHandler implementations.</param>
	/// <returns>The host application builder for chaining.</returns>
	public static WebApplicationBuilder AddWolverineEventBus(
		this WebApplicationBuilder builder,
		string outboxConnectionName,
		string outboxSchema,
		params Assembly[] eventHandlerAssemblies)
	{
		// Aspire's AddRabbitMQ("messaging") in the AppHost wires this in both
		// run and publish mode. A null/empty value here means misconfiguration
		// — fail fast at composition time so the deploy reports the error
		// before any traffic hits the API and silently no-ops every publish.
		// `!` is a compile-time hint to satisfy the GuardClause<string> overload —
		// the runtime check below is what actually rejects null/empty/whitespace.
		var rabbitConnection = builder.Configuration.GetConnectionString("messaging") ?? string.Empty;
		Ensure.That(rabbitConnection).IsNotNullOrWhiteSpace();

		var outboxConnection = builder.Configuration.GetConnectionString(outboxConnectionName) ?? string.Empty;
		Ensure.That(outboxConnection).IsNotNullOrWhiteSpace();

		Ensure.That(outboxSchema).IsNotNullOrWhiteSpace();

		// Each consuming app needs its OWN queue bound to the per-event fanout
		// exchange — otherwise Wolverine's default conventional routing names
		// queues by event type alone, so two modules subscribing to the same
		// event share one queue and become competing consumers (only one
		// module's handler runs per message). Prefixing with the application
		// name gives each module an isolated queue that receives every message.
		var moduleQueuePrefix = builder.Environment.ApplicationName ?? "yumney-app";

		builder.Host.UseWolverine(opts =>
		{
			opts.Discovery.DisableConventionalDiscovery();

			// MapWolverineEnvelopeStorage marks the envelope tables with
			// ExcludeFromMigrations(), so EF migrations can't manage them.
			// Keep Wolverine's idempotent CreateOrUpdate provisioner enabled
			// — it runs at API host startup, brings missing tables up to the
			// expected shape, and is a no-op after the first boot. Dev and
			// prod converge on the same logic; MigrationRunner remains
			// authoritative for the module's own EF-mapped tables.
			opts.AutoBuildMessageStorageOnStartup = AutoCreate.CreateOrUpdate;

			opts.PersistMessagesWithPostgresql(outboxConnection, outboxSchema);
			opts.UseEntityFrameworkCoreTransactions(TransactionMiddlewareMode.Eager);

			opts.UseRabbitMq(new Uri(rabbitConnection))
				.AutoProvision()
				.UseConventionalRouting(routing =>
				{
					routing.QueueNameForListener(eventType => $"{moduleQueuePrefix}.{eventType.FullName}");
				});

			foreach (var assembly in eventHandlerAssemblies)
			{
				RegisterBusEventConsumers(opts, assembly, typeof(IIntegrationEventHandler<>), typeof(IntegrationEventConsumer<>));
				RegisterBusEventConsumers(opts, assembly, typeof(IModuleEventHandler<>), typeof(ModuleEventConsumer<>));
			}
		});

		builder.Services.AddScoped<IEventBus, WolverineEventBus>();
		builder.Services.TryAddScoped<IInboxStore, NoOpInboxStore>();

		return builder;
	}

	/// <summary>
	/// Marker registration call to make the per-module outbox wiring explicit
	/// at the composition root. Wolverine's <c>IDbContextOutbox&lt;TContext&gt;</c>
	/// is registered automatically by <c>AddDbContextWithWolverineIntegration</c>
	/// in <see cref="SmartSolutionsLab.Yumney.Shared.Persistence.NpgsqlDbContextRegistration"/>;
	/// this method exists so module composition reads as
	/// "<c>AddYumneyDefaults(...).AddWolverineOutboxFor&lt;ShoppingDbContext&gt;()</c>"
	/// and lints catch missing registrations against the
	/// <see cref="Wolverine.EntityFrameworkCore.IDbContextOutbox{T}"/> dependency.
	/// </summary>
	/// <typeparam name="TContext">The DbContext that owns the outbox tables.</typeparam>
	/// <param name="builder">The host application builder.</param>
	/// <returns>The host application builder for chaining.</returns>
	public static WebApplicationBuilder AddWolverineOutboxFor<TContext>(this WebApplicationBuilder builder)
		where TContext : DbContext
	{
		// No additional service registrations — AddDbContextWithWolverineIntegration<TContext>
		// (called by the module's persistence wiring) registers IDbContextOutbox<TContext>.
		// This call exists for documentation and to surface the link between
		// "the module uses TContext" and "the module's events go through the outbox".
		return builder;
	}

	private static void RegisterBusEventConsumers(
		WolverineOptions opts,
		Assembly assembly,
		Type openHandlerInterface,
		Type openConsumerType)
	{
		var eventTypes = assembly.GetTypes()
			.SelectMany(type => type.GetInterfaces())
			.Where(iface => iface.IsGenericType && iface.GetGenericTypeDefinition() == openHandlerInterface)
			.Select(iface => iface.GetGenericArguments()[0])
			.Distinct();

		foreach (var eventType in eventTypes)
		{
			opts.Discovery.IncludeType(openConsumerType.MakeGenericType(eventType));
		}
	}
}
