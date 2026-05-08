using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Abstractions;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Common;

public class EntityTests
{
	private sealed class TestEntity : Entity<Guid>
	{
		public static TestEntity Create(Guid id)
		{
			var e = new TestEntity();
			e.Id = id;
			return e;
		}
	}

	private sealed class OtherEntity : Entity<Guid>
	{
		public static OtherEntity Create(Guid id)
		{
			var e = new OtherEntity();
			e.Id = id;
			return e;
		}
	}

	[Fact]
	public void Equals_DifferentConcreteType_SameId_ReturnsFalse()
	{
		var id = Guid.NewGuid();
		var entity = TestEntity.Create(id);
		var other = OtherEntity.Create(id);

		entity.Equals(other).Should().BeFalse();
	}

	[Fact]
	public void Equals_SameId_ReturnsTrue()
	{
		var id = Guid.NewGuid();
		var entity1 = TestEntity.Create(id);
		var entity2 = TestEntity.Create(id);

		entity1.Equals(entity2).Should().BeTrue();
	}

	[Fact]
	public void Equals_DifferentId_ReturnsFalse()
	{
		var entity1 = TestEntity.Create(Guid.NewGuid());
		var entity2 = TestEntity.Create(Guid.NewGuid());

		entity1.Equals(entity2).Should().BeFalse();
	}

	[Fact]
	public void Equals_Null_ReturnsFalse()
	{
		var entity = TestEntity.Create(Guid.NewGuid());

		entity.Equals(null).Should().BeFalse();
	}

	[Fact]
	public void Equals_DifferentType_ReturnsFalse()
	{
		var entity = TestEntity.Create(Guid.NewGuid());

		entity.Equals("not an entity").Should().BeFalse();
	}

	[Fact]
	public void Equals_SameReference_ReturnsTrue()
	{
		var entity = TestEntity.Create(Guid.NewGuid());

		entity.Equals(entity).Should().BeTrue();
	}

	[Fact]
	public void GetHashCode_SameId_ReturnsSameHash()
	{
		var id = Guid.NewGuid();
		var entity1 = TestEntity.Create(id);
		var entity2 = TestEntity.Create(id);

		entity1.GetHashCode().Should().Be(entity2.GetHashCode());
	}

	[Fact]
	public void OperatorEquals_BothNull_ReturnsTrue()
	{
		TestEntity? left = null;
		TestEntity? right = null;

		(left == right).Should().BeTrue();
	}

	[Fact]
	public void OperatorEquals_LeftNull_ReturnsFalse()
	{
		TestEntity? left = null;
		var right = TestEntity.Create(Guid.NewGuid());

		(left == right).Should().BeFalse();
	}

	[Fact]
	public void OperatorEquals_RightNull_ReturnsFalse()
	{
		var left = TestEntity.Create(Guid.NewGuid());
		TestEntity? right = null;

		(left == right).Should().BeFalse();
	}

	[Fact]
	public void OperatorNotEquals_DifferentId_ReturnsTrue()
	{
		var entity1 = TestEntity.Create(Guid.NewGuid());
		var entity2 = TestEntity.Create(Guid.NewGuid());

		(entity1 != entity2).Should().BeTrue();
	}
}
