using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Paging;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Common;

public class PagingOptionsDefaultsTests
{
	[Fact]
	public void Default_ReturnsPage1WithDefaultPageSize()
	{
		var options = PagingOptions.Default();

		options.Page.Value.Should().Be(PagingOptions.DefaultPage);
		options.PageSize.Value.Should().Be(PagingOptions.DefaultPageSize);
	}

	[Fact]
	public void Default_DefaultPage_IsOne()
	{
		PagingOptions.DefaultPage.Should().Be(1);
	}

	[Fact]
	public void Default_DefaultPageSize_IsTwenty()
	{
		PagingOptions.DefaultPageSize.Should().Be(20);
	}

	[Fact]
	public void From_DelegatesToOf()
	{
		var fromCtor = PagingOptions.From(3, 25);

		fromCtor.Page.Value.Should().Be(3);
		fromCtor.PageSize.Value.Should().Be(25);
	}
}
