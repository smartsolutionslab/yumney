using FluentAssertions;
using SmartSolutionsLab.Yumney.Recipes.Api;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Api.Tests;

public class StreamingJsonFieldDetectorTests
{
	[Fact]
	public void Consume_StringValueClosesInOneChunk_EmitsField()
	{
		var detector = new StreamingJsonFieldDetector();

		var events = detector.Consume("""{"title":"Pancakes","ingredients":""").ToList();

		events.Should().ContainSingle();
		events[0].Should().Be(("title", "Pancakes"));
	}

	[Fact]
	public void Consume_StringValueSpansMultipleChunks_EmitsWhenClosingQuoteArrives()
	{
		var detector = new StreamingJsonFieldDetector();

		var first = detector.Consume("""{"title":"Pan""").ToList();
		var second = detector.Consume("""cakes"},""").ToList();

		first.Should().BeEmpty();
		second.Should().ContainSingle().Which.Should().Be(("title", "Pancakes"));
	}

	[Fact]
	public void Consume_SameFieldSeenTwice_EmittedOnce()
	{
		var detector = new StreamingJsonFieldDetector();

		_ = detector.Consume("""{"title":"Pancakes",""").ToList();
		var later = detector.Consume("\"description\":\"Fluffy pancakes\"}").ToList();

		later.Should().ContainSingle();
		later[0].Field.Should().Be("description");
	}

	[Fact]
	public void Consume_NestedFieldWithSameName_DoesNotMatchAtDepthGreaterThanOne()
	{
		var detector = new StreamingJsonFieldDetector();

		// Nested `title` inside a nested object should not be emitted.
		var events = detector.Consume("""{"ingredients":[{"title":"nested"}],"title":"Real""").ToList();

		// Only the real top-level title (unfinished) — but its closing quote
		// hasn't arrived yet either, so nothing emits.
		events.Should().BeEmpty();
	}

	[Fact]
	public void Consume_EscapedQuoteInValue_KeptInDecodedString()
	{
		var detector = new StreamingJsonFieldDetector();

		var events = detector.Consume("""{"title":"Grandma's \"famous\" pancakes"}""").ToList();

		events.Should().ContainSingle();
		events[0].Value.Should().Be("""Grandma's "famous" pancakes""");
	}

	[Fact]
	public void Consume_FieldsOutOfOrder_EmittedInArrivalOrder()
	{
		var detector = new StreamingJsonFieldDetector();

		var events = detector.Consume("""{"description":"Classic","title":"Pancakes"}""").ToList();

		events.Should().HaveCount(2);
		events[0].Field.Should().Be("title"); // stringFields order: title first
		events[1].Field.Should().Be("description");
	}

	[Fact]
	public void Consume_UnknownField_Ignored()
	{
		var detector = new StreamingJsonFieldDetector();

		var events = detector.Consume("""{"rating":5,"title":"Pancakes"}""").ToList();

		events.Should().ContainSingle();
		events[0].Field.Should().Be("title");
	}

	[Fact]
	public void Consume_NonStringValue_Ignored()
	{
		var detector = new StreamingJsonFieldDetector();

		// difficulty in the schema is string; prepTimeMinutes is int and not
		// in the stringFields list — nothing emits for a numeric prepTime.
		var events = detector.Consume("""{"prepTimeMinutes":10,"difficulty":"easy"}""").ToList();

		events.Should().ContainSingle();
		events[0].Should().Be(("difficulty", "easy"));
	}
}
