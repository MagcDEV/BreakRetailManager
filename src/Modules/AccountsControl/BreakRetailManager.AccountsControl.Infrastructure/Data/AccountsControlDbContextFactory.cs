using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BreakRetailManager.AccountsControl.Infrastructure.Data;

public sealed class AccountsControlDbContextFactory : IDesignTimeDbContextFactory<AccountsControlDbContext>
{
    public AccountsControlDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AccountsControlDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Server=(localdb)\\mssqllocaldb;Database=BreakRetailManagerDesignTime;Trusted_Connection=True;TrustServerCertificate=True;";

        optionsBuilder.UseSqlServer(connectionString);
        return new AccountsControlDbContext(optionsBuilder.Options);
    }
}
