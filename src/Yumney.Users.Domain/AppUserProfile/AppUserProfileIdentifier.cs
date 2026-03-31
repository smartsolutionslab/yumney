using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

public sealed record AppUserProfileIdentifier : GuidIdentifier
{
    private AppUserProfileIdentifier(Guid value)
        : base(value)
    {
    }

    public static AppUserProfileIdentifier New() => new(Guid.NewGuid());

    public static AppUserProfileIdentifier From(Guid value) => new(value);

    public static AppUserProfileIdentifier? FromNullable(Guid? value) =>
        value.HasValue ? new AppUserProfileIdentifier(value.Value) : null;
}
