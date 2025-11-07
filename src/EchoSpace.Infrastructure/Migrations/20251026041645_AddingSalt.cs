using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EchoSpace.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddingSalt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotpEnabled",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "TotpEnabled",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
