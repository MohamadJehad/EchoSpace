using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EchoSpace.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserForAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsEmailVerified",
                table: "Users",
                newName: "LockoutEnabled");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "Users",
                newName: "EmailConfirmed");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "LockoutEnd",
                table: "Users",
                type: "datetimeoffset",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "Users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserName",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "LockoutEnabled",
                table: "Users",
                newName: "IsEmailVerified");

            migrationBuilder.RenameColumn(
                name: "EmailConfirmed",
                table: "Users",
                newName: "IsActive");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LockoutEnd",
                table: "Users",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldNullable: true);
        }
    }
}
