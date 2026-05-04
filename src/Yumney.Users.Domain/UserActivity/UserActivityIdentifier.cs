using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Users.Domain.UserActivity;

public sealed record UserActivityIdentifier : IValueObject
{
	public Guid Value { get; }

	private UserActivityIdentifier(Guid value)
	{
		Value = Ensure.That(value).IsNotEmpty().AndReturn();
	}

	public static UserActivityIdentifier New() => new(Guid.CreateVersion7());

	public static UserActivityIdentifier From(Guid value) => new(value);

	public override string ToString() => Value.ToString();
}
