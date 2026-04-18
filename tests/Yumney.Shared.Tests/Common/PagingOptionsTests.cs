using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Common;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Common;

public class PagingOptionsTests
{
	[Fact]
	public void From_ValidPageAndPageSize_ReturnsPagingOptions()
	{
		var paging = PagingOptions.Of(Page.From(3), PageSize.From(50));

		paging.Page.Value.Should().Be(3);
		paging.PageSize.Value.Should().Be(50);
	}

	[Fact]
	public void Skip_FirstPage_ReturnsZero()
	{
		var paging = PagingOptions.Of(Page.From(1), PageSize.From(20));

		paging.Skip.Should().Be(0);
	}

	[Fact]
	public void Skip_SecondPage_ReturnsPageSize()
	{
		var paging = PagingOptions.Of(Page.From(2), PageSize.From(20));

		paging.Skip.Should().Be(20);
	}

	[Fact]
	public void Skip_ThirdPageWithTenPerPage_ReturnsTwenty()
	{
		var paging = PagingOptions.Of(Page.From(3), PageSize.From(10));

		paging.Skip.Should().Be(20);
	}

	[Fact]
	public void DefaultPage_IsOne()
	{
		PagingOptions.DefaultPage.Should().Be(1);
	}

	[Fact]
	public void DefaultPageSize_IsTwenty()
	{
		PagingOptions.DefaultPageSize.Should().Be(20);
	}

	[Fact]
	public void MaxPageSize_IsOneHundred()
	{
		PagingOptions.MaxPageSize.Should().Be(100);
	}
}
