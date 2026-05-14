using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.Configurations;

internal sealed class IngredientBalanceReadItemConfiguration : IEntityTypeConfiguration<IngredientBalanceReadItem>
{
	public void Configure(EntityTypeBuilder<IngredientBalanceReadItem> builder)
	{
		builder.ToTable("IngredientBalanceReadItems");
		builder.HasKey(item => item.Id);
		builder.Property(item => item.OwnerId).HasMaxLength(255).IsRequired();
		builder.Property(item => item.ItemName).HasMaxLength(200).IsRequired();
		builder.Property(item => item.NameKey).HasMaxLength(200).IsRequired();
		builder.Property(item => item.Unit).HasMaxLength(20);
		builder.Property(item => item.Category).HasMaxLength(30).IsRequired();
		builder.Property(item => item.BoughtTotal).HasColumnType("numeric");
		builder.Property(item => item.ConsumedTotal).HasColumnType("numeric");
		builder.Property(item => item.RemovedTotal).HasColumnType("numeric");
		builder.Property(item => item.LastBoughtAt);
		builder.Property(item => item.LastUpdated).IsRequired();

		builder.Ignore(item => item.AtHome);

		builder.HasIndex(item => item.OwnerId);
		builder.HasIndex(item => new { item.OwnerId, item.NameKey, item.Unit });
	}
}
