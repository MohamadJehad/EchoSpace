using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EchoSpace.Infrastructure.Data
{
    public class EchoSpaceDbContextFactory : IDesignTimeDbContextFactory<EchoSpaceDbContext>
    {
        public EchoSpaceDbContext CreateDbContext(string[] args)
        {
            // Connection string for design-time migrations
            var connectionString = "Server=localhost;Database=EchoSpaceLocalDb;Trusted_Connection=True;MultipleActiveResultSets=true;Encrypt=False;TrustServerCertificate=True";

            // Create options builder
            var optionsBuilder = new DbContextOptionsBuilder<EchoSpaceDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            return new EchoSpaceDbContext(optionsBuilder.Options);
        }
    }
}

