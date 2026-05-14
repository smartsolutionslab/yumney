using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSolutionsLab.Yumney.Users.Domain.StaplesList;
using SmartSolutionsLab.Yumney.Users.Infrastructure.Persistence.Converters;

namespace SmartSolutionsLab.Yumney.Users.Infrastructure.Persistence.Configurations;

internal sealed class StaplesListConfiguration : IEntityTypeConfiguration<StaplesList>
{
	public void Configure(EntityTypeBuilder<StaplesList> builder)
	{
		builder.ToTable("StaplesLists");
		builder.HasKey(list => list.Id);
		builder.Property(list => list.Id)
			.HasConversion<StaplesListIdentifierConverter>();

		builder.Property(list => list.Owner)
			.HasConversion<StaplesOwnerIdentifierConverter>()
			.HasMaxLength(OwnerIdentifier.MaxLength)
			.IsRequired();

		builder.OwnsMany(list => list.Items, item =>
		{
			item.ToTable("StapleItems");
			item.WithOwner().HasForeignKey("StaplesListId");
			item.Property(staple => staple.Value)
				.HasColumnName("Name")
				.HasMaxLength(StapleItem.MaxLength);
		});

		builder.HasIndex(list => list.Owner).IsUnique();
		builder.Ignore(list => list.DomainEvents);
	}
}
