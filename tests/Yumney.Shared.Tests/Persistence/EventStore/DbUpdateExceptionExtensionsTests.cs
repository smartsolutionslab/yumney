using System.Data;
using System.Data.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Persistence.EventStore;

public class DbUpdateExceptionExtensionsTests
{
	[Theory]
	[InlineData("23505", true)] // Postgres unique_violation
	[InlineData("2627", true)] // SQL Server PK violation
	[InlineData("2601", true)] // SQL Server unique-index violation
	[InlineData("23503", false)] // foreign_key_violation
	[InlineData("42P01", false)] // undefined_table
	[InlineData(null, false)] // no SqlState — treated as non-violation
	public void IsUniqueViolation_MatchesProviderSqlStates(string? sqlState, bool expected)
	{
		var dbException = new FakeDbException(sqlState);
		var update = new DbUpdateException("save failed", dbException);

		update.IsUniqueViolation().Should().Be(expected);
	}

	[Fact]
	public void IsUniqueViolation_InnerNotDbException_ReturnsFalse()
	{
		var update = new DbUpdateException("save failed", new InvalidOperationException("not a provider exception"));

		update.IsUniqueViolation().Should().BeFalse();
	}

	[Fact]
	public void IsUniqueViolation_NoInner_ReturnsFalse()
	{
		var update = new DbUpdateException();

		update.IsUniqueViolation().Should().BeFalse();
	}

	private sealed class FakeDbException(string? sqlState) : DbException("simulated")
	{
		public override string? SqlState { get; } = sqlState;
	}
}
