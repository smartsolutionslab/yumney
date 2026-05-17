using System.Diagnostics.Metrics;
using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Events;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Events.Tests;

public class EventMetricsTests
{
	private sealed class FakeMeterFactory : IMeterFactory
	{
		public Meter Create(MeterOptions options) => new(options);

		public void Dispose()
		{
		}
	}

	[Fact]
	public void RecordCompletion_Success_EmitsCounterWithSuccessTag()
	{
		using var listener = new MeterListener();
		List<(long Value, KeyValuePair<string, object?>[] Tags)> samples = [];
		listener.InstrumentPublished = (instrument, l) =>
		{
			if (instrument.Meter.Name == EventMetrics.MeterName)
			{
				l.EnableMeasurementEvents(instrument);
			}
		};
		listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
			samples.Add((measurement, tags.ToArray())));
		listener.Start();

		var metrics = new EventMetrics(new FakeMeterFactory());
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
		using var listener = new MeterListener();
		List<KeyValuePair<string, object?>[]> samples = [];
		listener.InstrumentPublished = (instrument, l) =>
		{
			if (instrument.Meter.Name == EventMetrics.MeterName)
			{
				l.EnableMeasurementEvents(instrument);
			}
		};
		listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
			samples.Add(tags.ToArray()));
		listener.Start();

		var metrics = new EventMetrics(new FakeMeterFactory());
		metrics.RecordCompletion("integration", "RecipeDeleted", "ShoppingCleanupHandler", succeeded: false);

		samples.Should().ContainSingle();
		samples[0].Should().Contain(tag => tag.Key == "result" && (string)tag.Value! == "failure");
	}

	[Fact]
	public void MeterName_IsTheCanonicalYumneyEventsConstant()
	{
		EventMetrics.MeterName.Should().Be("Yumney.Events");
	}
}
