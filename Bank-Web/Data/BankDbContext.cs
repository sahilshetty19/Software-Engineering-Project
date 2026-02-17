using Bank.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace Bank.Web.Data;

public sealed class BankDbContext : DbContext
{
    public BankDbContext(DbContextOptions<BankDbContext> options) : base(options) { }

    public DbSet<BankCustomer> BankCustomers => Set<BankCustomer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Useful indexes (optional but good)
        modelBuilder.Entity<BankCustomer>()
            .HasIndex(x => new { x.FirstName, x.LastName, x.DateOfBirth });

        modelBuilder.Entity<BankCustomer>()
            .HasIndex(x => x.PPSN)
            .IsUnique();
    }
}
