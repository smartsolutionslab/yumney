using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.Configurations;

internal sealed class IngredientBalanceReadItemConfiguration : IEntityTypeConfiguration<IngredientBalanceReadItem>
{
	public void Configure(EntityTypeBuilder<IngredientBalanceReadItem> entity)
	{
		entity.ToTable("IngredientBalanceReadItems");
		entity.HasKey(e => e.Id);
		entity.Property(e => e.OwnerId).HasMaxLength(255).IsRequired();
		entity.Property(e => e.ItemName).HasMaxLength(200).IsRequired();
		entity.Property(e => e.NameKey).HasMaxLength(200).IsRequired();
		entity.Property(e => e.Unit).HasMaxLength(20);
		entity.Property(e => e.Category).HasMaxLength(30).IsRequired();
		entity.Property(e => e.BoughtTotal).HasColumnType("numeric");
		entity.Property(e => e.ConsumedTotal).HasColumnType("numeric");
		entity.Property(e => e.RemovedTotal).HasColumnType("numeric");
		entity.Property(e => e.LastUpdated).IsRequired();

		entity.Ignore(e => e.AtHome);

		entity.HasIndex(e => e.OwnerId);
		entity.HasIndex(e => new { e.OwnerId, e.NameKey, e.Unit });
	}
}
