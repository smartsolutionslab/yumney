using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSolutionsLab.Yumney.Shared.Events;

namespace SmartSolutionsLab.Yumney.Shared.Persistence;

/// <summary>
/// Shared EF Core configuration for the <see cref="InboxMessage"/> entity.
/// Modules adopting the inbox apply this configuration against their own
/// write-side DbContext and generate a migration.
/// </summary>
public sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
	public void Configure(EntityTypeBuilder<InboxMessage> builder)
	{
		builder.ToTable("InboxMessages");

		builder.HasKey(message => new { message.MessageId, message.ConsumerName });

		builder.Property(message => message.ConsumerName)
			.IsRequired()
			.HasMaxLength(256);

		builder.Property(message => message.ProcessedAt)
			.IsRequired();
	}
}
