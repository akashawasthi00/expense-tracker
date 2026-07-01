using ExpenseTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseTracker.Infrastructure.Persistence.Configurations;

public sealed class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Amount).HasColumnType("decimal(18,2)");
        builder.Property(e => e.Description).IsRequired().HasMaxLength(250);
        builder.Property(e => e.PaidByUserId).IsRequired().HasMaxLength(450);
        builder.Property(e => e.CreatedByUserId).IsRequired().HasMaxLength(450);
        builder.Property(e => e.SplitType).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(e => e.Date);
        builder.HasIndex(e => e.GroupId);

        builder.HasOne(e => e.Category)
            .WithMany(c => c.Expenses)
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Restrict); // keep categories even if referenced

        builder.HasMany(e => e.Shares)
            .WithOne(s => s.Expense)
            .HasForeignKey(s => s.ExpenseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
