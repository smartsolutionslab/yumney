using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Abstractions;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Common;

public class EntityNotFoundExceptionTests
{
	[Fact]
	public void Ctor_StoresEntityNameAndIdentifier()
	{
		var identifier = Guid.NewGuid();

		var exception = new EntityNotFoundException("Recipe", identifier);

		exception.EntityName.Should().Be("Recipe");
		exception.Identifier.Should().Be(identifier);
	}

	[Fact]
	public void Message_IncludesEntityNameAndIdentifier()
	{
		var exception = new EntityNotFoundException("Recipe", "abc-123");

		exception.Message.Should().Be("Recipe with identifier 'abc-123' was not found.");
	}
}
