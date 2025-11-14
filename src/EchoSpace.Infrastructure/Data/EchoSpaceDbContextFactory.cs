using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace EchoSpace.Infrastructure.Data
{
    public class EchoSpaceDbContextFactory : IDesignTimeDbContextFactory<EchoSpaceDbContext>
    {
        public EchoSpaceDbContext CreateDbContext(string[] args)
        {
            // Build configuration from appsettings files
            // Note: appsettings files are in the UI project, so we need to navigate up one level
            var basePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "EchoSpace.UI");
            var configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(basePath);
            
            // Try to load appsettings.Development.json first (for development)
            var devSettingsPath = Path.Combine(basePath, "appsettings.Development.json");
            if (File.Exists(devSettingsPath))
            {
                configurationBuilder.AddJsonFile("appsettings.Development.json", optional: false);
            }
            else
            {
                // Fallback to appsettings.json if Development file doesn't exist
                configurationBuilder.AddJsonFile("appsettings.json", optional: false);
            }
            
            var configuration = configurationBuilder.Build();

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

