using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Paging;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Common;

public class PagedResultExtensionsTests
{
	[Fact]
	public void AsPagedResult_ProjectsItemsAndPaging()
	{
		IReadOnlyList<string> items = ["a", "b", "c"];
		var paging = PagingOptions.Of(Page.From(2), PageSize.From(10));

		var result = items.AsPagedResult(ItemCount.From(42), paging);

		result.Items.Should().Equal("a", "b", "c");
		result.TotalCount.Should().Be(42);
		result.Page.Should().Be(2);
		result.PageSize.Should().Be(10);
	}

	[Fact]
	public void AsPagedResult_EmptyItems_PreservesTotalCount()
	{
		IReadOnlyList<string> items = [];
		var paging = PagingOptions.Of(Page.From(1), PageSize.From(20));

		var result = items.AsPagedResult(ItemCount.From(0), paging);

		result.Items.Should().BeEmpty();
		result.TotalCount.Should().Be(0);
	}

	[Fact]
	public void Map_AppliesSelectorToItems_PreservesPaging()
	{
		var source = new PagedResult<int>([1, 2, 3], TotalCount: 99, Page: 3, PageSize: 5);

		var mapped = source.Map(value => value.ToString(System.Globalization.CultureInfo.InvariantCulture));

		mapped.Items.Should().Equal("1", "2", "3");
		mapped.TotalCount.Should().Be(99);
		mapped.Page.Should().Be(3);
		mapped.PageSize.Should().Be(5);
	}
}
