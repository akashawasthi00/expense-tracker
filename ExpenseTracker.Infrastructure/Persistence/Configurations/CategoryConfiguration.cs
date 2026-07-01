using ExpenseTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseTracker.Infrastructure.Persistence.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(50);
        builder.HasIndex(c => c.Name).IsUnique();

        // Default categories available to every user from first run.
        builder.HasData(
            new Category { Id = 1, Name = "Food" },
            new Category { Id = 2, Name = "Transport" },
            new Category { Id = 3, Name = "Utilities" },
            new Category { Id = 4, Name = "Entertainment" },
            new Category { Id = 5, Name = "Shopping" },
            new Category { Id = 6, Name = "Other" });
    }
}
