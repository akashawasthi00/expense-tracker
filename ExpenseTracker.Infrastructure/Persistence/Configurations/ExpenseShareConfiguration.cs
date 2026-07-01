using ExpenseTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseTracker.Infrastructure.Persistence.Configurations;

public sealed class ExpenseShareConfiguration : IEntityTypeConfiguration<ExpenseShare>
{
    public void Configure(EntityTypeBuilder<ExpenseShare> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.UserId).IsRequired().HasMaxLength(450);
        builder.Property(s => s.Amount).HasColumnType("decimal(18,2)");
        builder.Property(s => s.Percentage).HasColumnType("decimal(5,2)");

        builder.HasIndex(s => s.UserId);
        builder.HasIndex(s => new { s.ExpenseId, s.UserId }).IsUnique();
    }
}
