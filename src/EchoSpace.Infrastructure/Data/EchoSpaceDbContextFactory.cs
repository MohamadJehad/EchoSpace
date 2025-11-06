using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace EchoSpace.Infrastructure.Data
{
    public class EchoSpaceDbContextFactory : IDesignTimeDbContextFactory<EchoSpaceDbContext>
    {
        public EchoSpaceDbContext CreateDbContext(string[] args)
        {
            // Build configuration from appsettings.json
            // Note: appsettings.json is in the UI project, so we need to navigate up one level
            var basePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "EchoSpace.UI");
            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json")
                .Build();

            // Get connection string from configuration
            var connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            // Create options builder
            var optionsBuilder = new DbContextOptionsBuilder<EchoSpaceDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            return new EchoSpaceDbContext(optionsBuilder.Options);
        }
    }
}

