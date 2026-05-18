using System.Diagnostics.Metrics;
using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Events;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Events.Tests;

public class EventMetricsTests
{
	[Fact]
	public void RecordCompletion_Success_EmitsCounterWithSuccessTag()
	{
		var (factory, getMeter) = CreateFactory();
		var metrics = new EventMetrics(factory);

		List<(long Value, KeyValuePair<string, object?>[] Tags)> samples = ListenForCounter(getMeter());
		metrics.RecordCompletion("domain", "RecipeImported", "ShoppingListProjectionHandler", succeeded: true);

		samples.Should().ContainSingle();
		samples[0].Value.Should().Be(1);
		samples[0].Tags.Should().Contain(tag => tag.Key == "result" && (string)tag.Value! == "success");
		samples[0].Tags.Should().Contain(tag => tag.Key == "event.category" && (string)tag.Value! == "domain");
		samples[0].Tags.Should().Contain(tag => tag.Key == "event.type" && (string)tag.Value! == "RecipeImported");
		samples[0].Tags.Should().Contain(tag => tag.Key == "handler.type" && (string)tag.Value! == "ShoppingListProjectionHandler");
	}

	[Fact]
	public void RecordCompletion_Failure_EmitsCounterWithFailureTag()
	{
		var (factory, getMeter) = CreateFactory();
		var metrics = new EventMetrics(factory);

		List<(long Value, KeyValuePair<string, object?>[] Tags)> samples = ListenForCounter(getMeter());
		metrics.RecordCompletion("integration", "RecipeDeleted", "ShoppingCleanupHandler", succeeded: false);

		samples.Should().ContainSingle();
		samples[0].Tags.Should().Contain(tag => tag.Key == "result" && (string)tag.Value! == "failure");
	}

	[Fact]
	public void MeterName_IsTheCanonicalYumneyEventsConstant()
	{
		EventMetrics.MeterName.Should().Be("Yumney.Events");
	}

	// Filter by Meter REFERENCE (not by name): MeterListener is process-wide
	// and InstrumentPublished fires for every pre-existing matching instrument
	// at Start(). Other tests in this assembly create their own EventMetrics
	// → multiple counters share the "Yumney.Events" meter name → a name-based
	// filter enables every instrument and one Add(1) produces multiple
	// samples in xUnit's parallel runner. The reference filter scopes us to
	// the meter we just created.
	private static List<(long Value, KeyValuePair<string, object?>[] Tags)> ListenForCounter(Meter meter)
	{
		List<(long Value, KeyValuePair<string, object?>[] Tags)> samples = [];
		var listener = new MeterListener
		{
			InstrumentPublished = (instrument, l) =>
			{
				if (ReferenceEquals(instrument.Meter, meter))
				{
					l.EnableMeasurementEvents(instrument);
				}
			},
		};
		listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
			samples.Add((measurement, tags.ToArray())));
		listener.Start();
		return samples;
	}

	private static (CapturingMeterFactory Factory, Func<Meter> GetMeter) CreateFactory()
	{
		var factory = new CapturingMeterFactory();
		return (factory, () => factory.LastCreated ?? throw new InvalidOperationException("Meter not yet created."));
	}

	private sealed class CapturingMeterFactory : IMeterFactory
	{
		public Meter? LastCreated { get; private set; }

		public Meter Create(MeterOptions options)
		{
			LastCreated = new Meter(options);
			return LastCreated;
		}

		public void Dispose()
		{
		}
	}
}
