using ExpenseTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseTracker.Infrastructure.Persistence.Configurations;

public sealed class SettlementConfiguration : IEntityTypeConfiguration<Settlement>
{
    public void Configure(EntityTypeBuilder<Settlement> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.FromUserId).IsRequired().HasMaxLength(450);
        builder.Property(s => s.ToUserId).IsRequired().HasMaxLength(450);
        builder.Property(s => s.Amount).HasColumnType("decimal(18,2)");
        builder.Property(s => s.Note).HasMaxLength(250);

        builder.HasIndex(s => s.GroupId);
    }
}
