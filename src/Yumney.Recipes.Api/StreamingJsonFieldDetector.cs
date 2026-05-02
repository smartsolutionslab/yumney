using System.Globalization;

namespace SmartSolutionsLab.Yumney.Recipes.Api;

/// <summary>
/// Consumes LLM streaming chunks, detects when a top-level scalar
/// string field has fully closed, and yields (name, value) pairs so
/// the SSE endpoint can emit a typed event per field as it arrives.
/// Only recognises string-valued scalars at depth 1; arrays and
/// nested objects are left for the final <c>done</c> event.
/// </summary>
#pragma warning disable SA1311
internal sealed class StreamingJsonFieldDetector
{
	private static readonly string[] stringFields =
	[
		"title",
		"description",
		"language",
		"difficulty",
		"imageUrl"
	];
#pragma warning restore SA1311

	private readonly HashSet<string> emitted = new(StringComparer.Ordinal);
	private readonly System.Text.StringBuilder buffer = new();

	/// <summary>
	/// Feeds the next LLM chunk in. Returns any newly detected
	/// <c>(fieldName, value)</c> pairs for top-level string scalars.
	/// Each field is reported at most once per detector instance.
	/// </summary>
	/// <param name="chunk">The next streaming text chunk from the LLM.</param>
	/// <returns>Zero or more (field, value) pairs that just closed in this chunk.</returns>
	public IEnumerable<(string Field, string Value)> Consume(string chunk)
	{
		buffer.Append(chunk);
		var snapshot = buffer.ToString();

		foreach (var field in stringFields)
		{
			if (emitted.Contains(field)) continue;
			if (TryReadTopLevelString(snapshot, field, out var value))
			{
				emitted.Add(field);
				yield return (field, value);
			}
		}
	}

	/// <summary>
	/// Finds <c>"field":"..."</c> at depth 1 and returns the decoded
	/// value if the closing quote has arrived. Ignores nested
	/// objects so an inner field of the same name does not match.
	/// </summary>
	private static bool TryReadTopLevelString(string json, string field, out string value)
	{
		value = string.Empty;
		var depth = 0;
		var i = 0;

		while (i < json.Length)
		{
			var c = json[i];

			if (c == '"')
			{
				// Parse string literal; skip over escape sequences.
				var (end, content) = ReadStringLiteral(json, i);
				if (end < 0) return false;

				// Only consider the string as a key if we're at depth 1
				// and the next non-whitespace is a colon.
				if (depth == 1 && content == field)
				{
					var colon = SkipWhitespace(json, end + 1);
					if (colon < json.Length && json[colon] == ':')
					{
						var valueStart = SkipWhitespace(json, colon + 1);
						if (valueStart < json.Length && json[valueStart] == '"')
						{
							var (valueEnd, decoded) = ReadStringLiteral(json, valueStart);
							if (valueEnd < 0) return false;

							value = decoded;
							return true;
						}
					}
				}

				i = end + 1;
				continue;
			}

			if (c == '{' || c == '[') depth++;
			else if (c == '}' || c == ']') depth--;

			i++;
		}

		return false;
	}

	private static int SkipWhitespace(string json, int from)
	{
		while (from < json.Length && char.IsWhiteSpace(json[from])) from++;
		return from;
	}

	/// <summary>
	/// Reads a JSON string literal starting at <paramref name="start"/>
	/// (which must point to an opening quote). Returns the index of
	/// the closing quote and the decoded content. Returns (-1, "") if
	/// the closing quote hasn't arrived yet.
	/// </summary>
	private static (int End, string Content) ReadStringLiteral(string json, int start)
	{
		var sb = new System.Text.StringBuilder();
		var i = start + 1;

		while (i < json.Length)
		{
			var c = json[i];
			if (c == '\\')
			{
				if (i + 1 >= json.Length) return (-1, string.Empty);
				var escaped = json[i + 1];

				if (escaped == 'u')
				{
					// Unicode escape: need 4 hex digits. If the stream hasn't
					// delivered them yet, signal incomplete so the caller
					// waits for the next chunk.
					if (i + 5 >= json.Length) return (-1, string.Empty);
					var hex = json.AsSpan(i + 2, 4);
					if (!ushort.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var code))
					{
						return (-1, string.Empty);
					}

					sb.Append((char)code);
					i += 6;
					continue;
				}

				sb.Append(escaped switch
				{
					'"' => '"',
					'\\' => '\\',
					'/' => '/',
					'n' => '\n',
					'r' => '\r',
					't' => '\t',
					'b' => '\b',
					'f' => '\f',
					_ => escaped,
				});
				i += 2;
				continue;
			}

			if (c == '"') return (i, sb.ToString());

			sb.Append(c);
			i++;
		}

		return (-1, string.Empty);
	}
}
