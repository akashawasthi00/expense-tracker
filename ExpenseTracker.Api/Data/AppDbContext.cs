using ExpenseTracker.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Expense> Expenses => Set<Expense>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Unique index on User.Email (SQL: UNIQUE INDEX)
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // Index on Expense.Date for fast date-range / monthly reports
        modelBuilder.Entity<Expense>()
            .HasIndex(e => e.Date);

        // Foreign keys with explicit delete behavior
        modelBuilder.Entity<Expense>()
            .HasOne(e => e.User)
            .WithMany(u => u.Expenses)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Expense>()
            .HasOne(e => e.Category)
            .WithMany(c => c.Expenses)
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Seed data so the app has something to query immediately
        modelBuilder.Entity<User>().HasData(
            new User { Id = 1, Name = "Demo User", Email = "demo@example.com" });

        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Food" },
            new Category { Id = 2, Name = "Transport" },
            new Category { Id = 3, Name = "Utilities" },
            new Category { Id = 4, Name = "Entertainment" });
    }
}
