using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace EchoSpace.Infrastructure.Data
{
    public class EchoSpaceDbContextFactory : IDesignTimeDbContextFactory<EchoSpaceDbContext>
    {
        public EchoSpaceDbContext CreateDbContext(string[] args)
        {
            string? connectionString = null;

            // Priority 1: Check environment variable (for CI/CD)
            connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                ?? Environment.GetEnvironmentVariable("AZURE_SQL_CONNECTION_STRING");

            // Priority 2: Try to get from command line args (when using --connection parameter)
            if (string.IsNullOrEmpty(connectionString) && args != null && args.Length > 0)
            {
                // EF Core passes connection string via args when using --connection
                foreach (var arg in args)
                {
                    if (arg.StartsWith("--connection", StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = arg.Split('=', 2);
                        if (parts.Length == 2)
                        {
                            connectionString = parts[1];
                            break;
                        }
                    }
                }
            }

            // Priority 3: Try to load from appsettings files (for local development)
            if (string.IsNullOrEmpty(connectionString))
            {
                var basePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "EchoSpace.UI");
                var configurationBuilder = new ConfigurationBuilder()
                    .SetBasePath(basePath);
                
                // Try to load appsettings.Development.json first (for development)
                var devSettingsPath = Path.Combine(basePath, "appsettings.Development.json");
                if (File.Exists(devSettingsPath))
                {
                    configurationBuilder.AddJsonFile("appsettings.Development.json", optional: true);
                }
                else
                {
                    // Fallback to appsettings.json if Development file doesn't exist
                    configurationBuilder.AddJsonFile("appsettings.json", optional: true);
                }
                
                var configuration = configurationBuilder.Build();
                connectionString = configuration.GetConnectionString("DefaultConnection");
            }

            // If still no connection string, throw error
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException(
                    "Connection string 'DefaultConnection' not found. " +
                    "Please set it via: " +
                    "1) Environment variable 'ConnectionStrings__DefaultConnection' or 'AZURE_SQL_CONNECTION_STRING', " +
                    "2) --connection parameter, or " +
                    "3) appsettings.json file.");
            }

            // Create options builder
            var optionsBuilder = new DbContextOptionsBuilder<EchoSpaceDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            return new EchoSpaceDbContext(optionsBuilder.Options);
        }
    }
}

