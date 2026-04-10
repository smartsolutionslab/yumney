using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSolutionsLab.Yumney.Users.Domain.StaplesList;
using SmartSolutionsLab.Yumney.Users.Infrastructure.Persistence.Converters;

namespace SmartSolutionsLab.Yumney.Users.Infrastructure.Persistence.Configurations;

internal sealed class StaplesListConfiguration : IEntityTypeConfiguration<StaplesList>
{
    public void Configure(EntityTypeBuilder<StaplesList> entity)
    {
        entity.ToTable("StaplesLists");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id)
            .HasConversion<StaplesListIdentifierConverter>();

        entity.Property(e => e.Owner)
            .HasConversion<StaplesOwnerIdentifierConverter>()
            .HasMaxLength(OwnerIdentifier.MaxLength)
            .IsRequired();

        entity.OwnsMany(e => e.Items, item =>
        {
            item.ToTable("StapleItems");
            item.WithOwner().HasForeignKey("StaplesListId");
            item.Property(i => i.Value)
                .HasColumnName("Name")
                .HasMaxLength(StapleItem.MaxLength);
        });

        entity.HasIndex(e => e.Owner).IsUnique();
        entity.Ignore(e => e.DomainEvents);
    }
}
