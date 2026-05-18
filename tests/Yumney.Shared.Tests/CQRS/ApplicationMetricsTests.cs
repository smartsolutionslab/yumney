using System.Diagnostics.Metrics;
using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.CQRS.Diagnostics;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.CQRS;

public class ApplicationMetricsTests
{
	[Fact]
	public void RecordExecution_IncrementsExecutedCounterWithTags()
	{
		var (factory, getMeter) = CreateFactory();
		var metrics = new ApplicationMetrics(factory);
		var meter = getMeter();

		List<(long Value, KeyValuePair<string, object?>[] Tags)> samples = ListenForLongInstrument(meter, "yumney.handlers.executed");
		metrics.RecordExecution("ImportHandler", "ImportRecipeCommand", "success", durationMs: 12.5);

		samples.Should().ContainSingle();
		samples[0].Value.Should().Be(1);
		samples[0].Tags.Should().Contain(tag => tag.Key == "handler_name" && (string)tag.Value! == "ImportHandler");
		samples[0].Tags.Should().Contain(tag => tag.Key == "command_type" && (string)tag.Value! == "ImportRecipeCommand");
		samples[0].Tags.Should().Contain(tag => tag.Key == "result" && (string)tag.Value! == "success");
	}

	[Fact]
	public void RecordExecution_RecordsHistogramWithDurationAndTags()
	{
		var (factory, getMeter) = CreateFactory();
		var metrics = new ApplicationMetrics(factory);
		var meter = getMeter();

		List<(double Value, KeyValuePair<string, object?>[] Tags)> samples = ListenForDoubleInstrument(meter, "yumney.handlers.duration");
		metrics.RecordExecution("RateHandler", "RateRecipeCommand", "failure", durationMs: 42.3);

		samples.Should().ContainSingle();
		samples[0].Value.Should().Be(42.3);
		samples[0].Tags.Should().Contain(tag => tag.Key == "result" && (string)tag.Value! == "failure");
	}

	[Fact]
	public void MeterName_IsTheCanonicalYumneyApplicationConstant()
	{
		ApplicationMetrics.MeterName.Should().Be("Yumney.Application");
	}

	// Filter by Meter REFERENCE (not by name): MeterListener is process-wide
	// and InstrumentPublished fires for every pre-existing matching instrument
	// at Start(). Other tests in this assembly create their own ApplicationMetrics
	// → multiple counters share the "Yumney.Application" meter name → a name-based
	// filter enables every instrument and one Add(1) produces multiple
	// samples in xUnit's parallel runner. The reference filter scopes us to
	// the meter we just created.
	private static List<(long Value, KeyValuePair<string, object?>[] Tags)> ListenForLongInstrument(Meter meter, string instrumentName)
	{
		List<(long Value, KeyValuePair<string, object?>[] Tags)> samples = [];
		var listener = new MeterListener
		{
			InstrumentPublished = (instrument, l) =>
			{
				if (ReferenceEquals(instrument.Meter, meter) && instrument.Name == instrumentName)
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

	private static List<(double Value, KeyValuePair<string, object?>[] Tags)> ListenForDoubleInstrument(Meter meter, string instrumentName)
	{
		List<(double Value, KeyValuePair<string, object?>[] Tags)> samples = [];
		var listener = new MeterListener
		{
			InstrumentPublished = (instrument, l) =>
			{
				if (ReferenceEquals(instrument.Meter, meter) && instrument.Name == instrumentName)
				{
					l.EnableMeasurementEvents(instrument);
				}
			},
		};
		listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, _) =>
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
