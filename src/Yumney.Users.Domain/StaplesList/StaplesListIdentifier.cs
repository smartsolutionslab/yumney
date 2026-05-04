using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Users.Domain.StaplesList;

public sealed record StaplesListIdentifier : IValueObject
{
	public Guid Value { get; }

	private StaplesListIdentifier(Guid value)
	{
		Value = Ensure.That(value).IsNotEmpty().AndReturn();
	}

	public static StaplesListIdentifier New() => new(Guid.CreateVersion7());

	public static StaplesListIdentifier From(Guid value) => new(value);

	public override string ToString() => Value.ToString();
}
