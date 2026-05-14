using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.Configurations;

internal sealed class ShoppingListSummaryReadItemConfiguration : IEntityTypeConfiguration<ShoppingListSummaryReadItem>
{
	public void Configure(EntityTypeBuilder<ShoppingListSummaryReadItem> builder)
	{
		builder.ToTable("ShoppingListSummaryReadItems");
		builder.HasKey(summary => summary.Id);
		builder.Property(summary => summary.OwnerId).HasMaxLength(255).IsRequired();
		builder.Property(summary => summary.Title).HasMaxLength(200).IsRequired();
		builder.Property(summary => summary.CreatedAt).IsRequired();
		builder.Property(summary => summary.LastUpdated).IsRequired();

		builder.HasIndex(summary => summary.OwnerId);
		builder.HasIndex(summary => new { summary.OwnerId, summary.CreatedAt });
		builder.HasIndex(summary => new { summary.OwnerId, summary.Title });
	}
}
