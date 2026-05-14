using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.Configurations;

internal sealed class ShoppingListItemReadItemConfiguration : IEntityTypeConfiguration<ShoppingListItemReadItem>
{
	public void Configure(EntityTypeBuilder<ShoppingListItemReadItem> builder)
	{
		builder.ToTable("ShoppingListItemReadItems");
		builder.HasKey(item => item.Id);
		builder.Property(item => item.OwnerId).HasMaxLength(255).IsRequired();
		builder.Property(item => item.Name).HasMaxLength(200).IsRequired();
		builder.Property(item => item.QuantityUnit).HasMaxLength(50);
		builder.Property(item => item.Category).HasMaxLength(50).IsRequired().HasDefaultValue("other");
		builder.Property(item => item.CreatedAt).IsRequired();
		builder.Property(item => item.LastUpdated).IsRequired();

		builder.HasIndex(item => item.ListId);
		builder.HasIndex(item => new { item.OwnerId, item.ListId });
	}
}
