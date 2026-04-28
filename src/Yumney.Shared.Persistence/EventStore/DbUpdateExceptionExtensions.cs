using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;

public static class DbUpdateExceptionExtensions
{
	/// <summary>
	/// Returns true when the database surfaced a unique-constraint violation.
	/// PostgreSQL uses SqlState 23505; SQL Server uses 2627/2601 (mapped here too).
	/// </summary>
	/// <param name="exception">The EF Core update exception to inspect.</param>
	/// <returns>True if the inner provider exception indicates a unique-key collision.</returns>
	public static bool IsUniqueViolation(this DbUpdateException exception)
	{
		if (exception.InnerException is not DbException dbException) return false;

		return dbException.SqlState switch
		{
			"23505" => true,
			"2627" or "2601" => true,
			_ => false,
		};
	}
}
