using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

public sealed record AppUserProfileIdentifier
{
    public Guid Value { get; }

    public AppUserProfileIdentifier(Guid value)
    {
        Value = Ensure.That(value).IsNotEmpty().AndReturn();
    }

    public static AppUserProfileIdentifier New() => new(Guid.NewGuid());

    public static AppUserProfileIdentifier From(Guid value) => new(value);

    public static AppUserProfileIdentifier? FromNullable(Guid? value) =>
        value.HasValue ? new AppUserProfileIdentifier(value.Value) : null;

    public override string ToString() => Value.ToString();
}
