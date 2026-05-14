using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSolutionsLab.Yumney.Users.Domain.UserActivity;
using SmartSolutionsLab.Yumney.Users.Infrastructure.Persistence.Converters;

namespace SmartSolutionsLab.Yumney.Users.Infrastructure.Persistence.Configurations;

internal sealed class UserActivityConfiguration : IEntityTypeConfiguration<UserActivity>
{
	public void Configure(EntityTypeBuilder<UserActivity> builder)
	{
		builder.ToTable("UserActivities");
		builder.HasKey(activity => activity.Id);
		builder.Property(activity => activity.Id)
			.HasConversion<UserActivityIdentifierConverter>();

		builder.Property(activity => activity.Owner)
			.HasConversion<UserActivityOwnerIdentifierConverter>()
			.HasMaxLength(OwnerIdentifier.MaxLength)
			.IsRequired();

		builder.Property(activity => activity.Type)
			.HasConversion<ActivityTypeConverter>()
			.HasMaxLength(50)
			.IsRequired();

		builder.Property(activity => activity.RecipeIdentifier)
			.HasConversion(
				snapshot => snapshot == null ? (Guid?)null : snapshot.Value,
				value => value.HasValue ? RecipeIdentifierSnapshot.From(value.Value) : null);

		builder.Property(activity => activity.RecipeTitle)
			.HasMaxLength(RecipeTitleSnapshot.MaxLength)
			.HasConversion(
				snapshot => snapshot == null ? null : snapshot.Value,
				value => value == null ? null : RecipeTitleSnapshot.From(value));

		builder.Property(activity => activity.OccurredAt)
			.IsRequired();

		builder.HasIndex(activity => new { activity.Owner, activity.OccurredAt })
			.IsDescending(false, true);
	}
}
