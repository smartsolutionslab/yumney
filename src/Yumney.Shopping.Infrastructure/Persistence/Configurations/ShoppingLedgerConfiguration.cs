using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.Converters;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.Configurations;

internal sealed class ShoppingLedgerConfiguration : IEntityTypeConfiguration<ShoppingLedger>
{
    public void Configure(EntityTypeBuilder<ShoppingLedger> entity)
    {
        entity.ToTable("ShoppingLedgers");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id)
            .HasConversion<ShoppingLedgerIdentifierConverter>();

        entity.Property(e => e.Owner)
            .HasConversion<ShoppingOwnerIdentifierConverter>()
            .HasMaxLength(255)
            .IsRequired();

        entity.OwnsMany(e => e.Transactions, tx =>
        {
            tx.ToTable("LedgerTransactions");
            tx.WithOwner().HasForeignKey("ShoppingLedgerId");

            tx.Property(t => t.Id)
                .HasConversion<LedgerTransactionIdentifierConverter>();

            tx.Property(t => t.ItemName)
                .HasConversion<ItemNameConverter>()
                .HasMaxLength(200)
                .IsRequired();

            tx.Property(t => t.Action)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            tx.Property(t => t.Quantity).IsRequired();

            tx.Property(t => t.Unit).HasMaxLength(20);

            tx.Property(t => t.Source)
                .HasConversion<TransactionSourceConverter>()
                .HasMaxLength(500)
                .IsRequired();

            tx.Property(t => t.OccurredAt).IsRequired();

            tx.HasIndex(t => t.OccurredAt);
        });

        entity.HasIndex(e => e.Owner).IsUnique();
        entity.Ignore(e => e.DomainEvents);
    }
}
