using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EchoSpace.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Users table already created in AddUserEntity migration
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Users table will be dropped in AddUserEntity migration
        }
    }
}
