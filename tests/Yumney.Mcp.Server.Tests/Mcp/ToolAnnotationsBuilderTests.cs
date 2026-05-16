using FluentAssertions;
using SmartSolutionsLab.Yumney.Mcp.Server.Mcp;
using Xunit;

namespace SmartSolutionsLab.Yumney.Mcp.Server.Tests.Mcp;

public class ToolAnnotationsBuilderTests
{
	[Theory]
	[InlineData("GET")]
	[InlineData("get")]
	[InlineData("Get")]
	public void FromHttpMethod_Get_IsReadOnlyAndIdempotent(string method)
	{
		var annotations = ToolAnnotationsBuilder.FromHttpMethod(method);

		annotations.ReadOnlyHint.Should().BeTrue();
		annotations.IdempotentHint.Should().BeTrue();
		annotations.DestructiveHint.Should().BeFalse();
	}

	[Theory]
	[InlineData("DELETE")]
	[InlineData("delete")]
	public void FromHttpMethod_Delete_IsDestructiveAndIdempotentButNotReadOnly(string method)
	{
		var annotations = ToolAnnotationsBuilder.FromHttpMethod(method);

		annotations.DestructiveHint.Should().BeTrue();
		annotations.IdempotentHint.Should().BeTrue();
		annotations.ReadOnlyHint.Should().BeFalse();
	}

	[Fact]
	public void FromHttpMethod_Put_IsIdempotentButNotReadOnlyOrDestructive()
	{
		var annotations = ToolAnnotationsBuilder.FromHttpMethod("PUT");

		annotations.IdempotentHint.Should().BeTrue();
		annotations.ReadOnlyHint.Should().BeFalse();
		annotations.DestructiveHint.Should().BeFalse();
	}

	[Theory]
	[InlineData("POST")]
	[InlineData("PATCH")]
	public void FromHttpMethod_PostOrPatch_HasNoHintsOtherThanOpenWorld(string method)
	{
		var annotations = ToolAnnotationsBuilder.FromHttpMethod(method);

		annotations.ReadOnlyHint.Should().BeFalse();
		annotations.IdempotentHint.Should().BeFalse();
		annotations.DestructiveHint.Should().BeFalse();
	}

	[Theory]
	[InlineData("GET")]
	[InlineData("POST")]
	[InlineData("PUT")]
	[InlineData("DELETE")]
	[InlineData("PATCH")]
	public void FromHttpMethod_OpenWorldHint_IsAlwaysFalse(string method)
	{
		var annotations = ToolAnnotationsBuilder.FromHttpMethod(method);

		annotations.OpenWorldHint.Should().BeFalse();
	}
}
