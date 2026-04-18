using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;

[CollectionDefinition(Name)]
#pragma warning disable CA1711 // xUnit convention requires Collection suffix
public class AspireCollection : ICollectionFixture<AspireFixture>
{
	public const string Name = "Aspire";
}
