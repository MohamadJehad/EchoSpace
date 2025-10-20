using EchoSpace.Models;
using Microsoft.EntityFrameworkCore;

namespace EchoSpace.Data
{
    public class EchoSpaceDbContext : DbContext
    {
        public EchoSpaceDbContext(DbContextOptions<EchoSpaceDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}


