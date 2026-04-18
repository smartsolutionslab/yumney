using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Users.Domain.StaplesList;
using SmartSolutionsLab.Yumney.Users.Infrastructure.Services;
using Xunit;

namespace SmartSolutionsLab.Yumney.Users.Infrastructure.Tests.Services;

public class StaplesProviderTests
{
	private readonly IStaplesListRepository staplesLists = Substitute.For<IStaplesListRepository>();
	private readonly StaplesProvider provider;

	public StaplesProviderTests()
	{
		provider = new StaplesProvider(staplesLists);
	}

	[Fact]
	public async Task GetStapleNamesAsync_NoStaplesListExists_ReturnsDefaults()
	{
		staplesLists.FindByOwnerAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<CancellationToken>())
			.Returns((StaplesList?)null);

		var result = await provider.GetStapleNamesAsync("user-123");

		result.Should().Contain("salt");
		result.Should().Contain("pepper");
		result.Should().Contain("olive oil");
		result.Count.Should().Be(StaplesList.DefaultItems.Count);
	}

	[Fact]
	public async Task GetStapleNamesAsync_CustomStaplesListExists_ReturnsCustomItems()
	{
		var owner = OwnerIdentifier.From("user-123");
		var list = StaplesList.Create(owner);
		list.AddItem(StapleItem.From("rice"));
		list.AddItem(StapleItem.From("soy sauce"));

		staplesLists.FindByOwnerAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<CancellationToken>())
			.Returns(list);

		var result = await provider.GetStapleNamesAsync("user-123");

		result.Should().HaveCount(2);
		result.Should().Contain("rice");
		result.Should().Contain("soy sauce");
	}

	[Fact]
	public async Task GetStapleNamesAsync_ReturnsCaseInsensitiveSet()
	{
		staplesLists.FindByOwnerAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<CancellationToken>())
			.Returns((StaplesList?)null);

		var result = await provider.GetStapleNamesAsync("user-123");

		result.Contains("Salt").Should().BeTrue();
		result.Contains("SALT").Should().BeTrue();
		result.Contains("salt").Should().BeTrue();
	}
}
