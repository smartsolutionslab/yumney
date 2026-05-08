using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace SmartSolutionsLab.Yumney.Shared.Events;

/// <summary>
/// OpenTelemetry counters for the in-process dispatchers. The dispatcher
/// isolates handler failures (one handler's exception must not block its
/// peers) and only logs them, so a swallowed failure has no other signal.
/// This counter gives that signal an SLO/alert surface — emit
/// <c>yumney.events.handler.failures</c> on every caught exception.
/// </summary>
public sealed class EventMetrics
{
	public const string MeterName = "Yumney.Events";

	private readonly Counter<long> handlerCompleted;

	public EventMetrics(IMeterFactory meterFactory)
	{
		var meter = meterFactory.Create(MeterName);
		handlerCompleted = meter.CreateCounter<long>(
			"yumney.events.handler.completed",
			description: "In-process event handler executions, tagged by result (success|failure).");
	}

	public void RecordCompletion(string eventCategory, string eventType, string handlerType, bool succeeded)
	{
		TagList tags = new()
		{
			{ "event.category", eventCategory },
			{ "event.type", eventType },
			{ "handler.type", handlerType },
			{ "result", succeeded ? "success" : "failure" },
		};

		handlerCompleted.Add(1, tags);
	}
}
