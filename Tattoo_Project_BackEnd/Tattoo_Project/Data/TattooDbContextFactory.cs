using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Tattoo_Project.Data;

public sealed class TattooDbContextFactory : IDesignTimeDbContextFactory<TattooDbContext>
{
    public TattooDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = "Server=(localdb)\\mssqllocaldb;Database=InkRouteDesignTime;Trusted_Connection=True;MultipleActiveResultSets=true";
        }

        var options = new DbContextOptionsBuilder<TattooDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new TattooDbContext(options);
    }
}
